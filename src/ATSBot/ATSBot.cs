using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ----------------------------------------------------------------------------------
// ASS AKA Aggresive Seeker Sharpshooter
// ----------------------------------------------------------------------------------
// This bot utilizes the strategy to find the optimal target. Every turn, scan for 
// enemies and immediately target the one that is closest and lowest on energy. 
// Fire dynamic firepower based on probability.
// ----------------------------------------------------------------------------------
// To determine the optimal target, the bot uses simple greedy behaviour and find
// enemy with the highest value of:
// (DISTANCE_WEIGHT / distance) + (ENERGY_WEIGHT / energy)
// The lower the distance and the energy, the higher the value
// 
// To determine the firepower, the bot calculate the value of each firepower
// starting from 0.1 to 3. The firepower value is affected by:
// - distance = 1 - distance / max distance
// - enemy stability = 1 - enemy erraticness
// - enemy speed = 1 - enemy speed / max speed
// - bullet travel time = exp(-lambda * bullet travel time) (use decaying value)
// All the above factor determine the hit probability
// To determine the value of a firepower, the bot calculate the risk and reward:
// value = hit probability * damage - cost (cost = firepower)
// The bot then fire the firepower with the highest value
// ----------------------------------------------------------------------------------

public class ATSBot : Bot 
{
    private Dictionary<int, EnemyInfo> enemies = new Dictionary<int, EnemyInfo>();
    private double MAX_DISTANCE;
    private const double LAMDA = 0.01;
    private const double MAX_VELOCITY = 8.0;
    private const double RADIANS_TO_DEGREE = 180.0 / Math.PI;
    private const double DEGREE_TO_RADIANS = Math.PI / 180.0;
    private const double LOWEST_FIREPOWER = 0.1;
    private const double MAX_ENERGY = 3.0;
    private const double FIREPOWER_INCREMENT = 0.1;
    private const double DISTANCE_WEIGHT = 1.0;
    private const double ENERGY_WEIGHT = 5.0;
    private Random randomGenerator = new Random();
    static void Main(string[] args)
    {
        new ATSBot().Start();
    }

    ATSBot() : base(BotInfo.FromFile("ATSBot.json")) { }

    public override void Run()
    {
        MAX_DISTANCE = Math.Sqrt(Math.Pow(ArenaHeight, 2) + Math.Pow(ArenaWidth, 2));
        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        AddCustomEvent(new FullRadarScan(this));

        TurnRadarRight(360);
    }

    public override void OnCustomEvent(CustomEvent evt)
    {
        if (evt.Condition is FullRadarScan)
        {
            // Calculating Target
            EnemyInfo target = FindTarget();

            if (target != null)
            {
                // Aiming and Firing logic
                double distance = DistanceTo(target.XPosition, target.YPosition);
                double firepower = ChooseFirepower(target, distance);
                double travelTime = distance / (20 - 3 * firepower); // Based on game rule

                double enemyRadians = target.Direction * DEGREE_TO_RADIANS;
                double predictedX = target.XPosition + target.Speed * Math.Cos(enemyRadians) * travelTime;
                double predictedY = target.YPosition + target.Speed * Math.Sin(enemyRadians) * travelTime;

                double gunAngle = RelativeEnemyAngle(predictedX, predictedY, true);

                TurnGunLeft(NormalizeRelativeAngle(gunAngle));
                Fire(firepower);

                // Movement Behaviour
                if (distance > 300)
                {
                    double tankAngle = RelativeEnemyAngle(predictedX, predictedY);
                    TurnLeft(tankAngle);
                    Forward(Math.Min(distance - 250, 100));
                }
                else
                {
                    TurnRight(90);
                    Forward(50);
                }
            }
            TurnRadarRight(360);
        }
    }

    private double ChooseFirepower(EnemyInfo enemy, double distance)
    {
        double bestValue = double.MinValue;
        double bestFirepower = LOWEST_FIREPOWER;

        double maxCandidate = Math.Min(MAX_ENERGY, Energy);
        double pDistance = Math.Max(0, 1 - distance / MAX_DISTANCE);
        double stability = 1 - enemy.Erraticness;
        double speedFactor = Math.Max(0.1 , 1 - enemy.Speed / MAX_VELOCITY);

        for (double candidate = LOWEST_FIREPOWER; candidate <= maxCandidate; candidate += FIREPOWER_INCREMENT)
        {
            double bulletSpeed = 20 - 3 * candidate; // based on game rule
            double travelTime = distance / bulletSpeed;

            double pTravel = Math.Exp(-LAMDA * travelTime);

            double hitProbability = stability * pDistance * pTravel * speedFactor;

            // based on game rule
            double damage = (candidate <= 1) ? (4 * candidate) : (4 * candidate + 2 * (candidate - 1));

            double value = hitProbability * damage - candidate;

            if (value > bestValue)
            {
                bestValue = value;
                bestFirepower = candidate;
            }
        }


        return bestFirepower;
    }

    public override void OnScannedBot(ScannedBotEvent scannedBot)
    {
        int id = scannedBot.ScannedBotId;
        EnemyInfo enemy;
        if (!enemies.ContainsKey(id))
        {
            enemy = new EnemyInfo
            {
                Id = scannedBot.ScannedBotId,
                XPosition = scannedBot.X,
                YPosition = scannedBot.Y,
                Energy = scannedBot.Energy,
                Direction = scannedBot.Direction,
                Speed = scannedBot.Speed
            };
        }
        else
        {
            enemy = enemies[id];
            double directionDiff = Math.Abs(NormalizeRelativeAngle(scannedBot.Direction - enemy.Direction));
            double directionFactor = directionDiff / 180.0;
            double speedDiff = Math.Abs(scannedBot.Speed - enemy.Speed);
            double speedFactor = speedDiff / MAX_VELOCITY;
            double Erraticness = (directionFactor + speedFactor) / 2.0; // average
            enemy.Erraticness = (enemy.Erraticness + Erraticness) / 2.0; // average to smooth the erraticness
            enemy.XPosition = scannedBot.X;
            enemy.YPosition = scannedBot.Y;
            enemy.Direction = scannedBot.Direction;
            enemy.Speed = scannedBot.Speed;
            enemy.Energy = scannedBot.Energy;
        }

        enemies[id] = enemy;
        Rescan();
    }

    public override void OnBotDeath(BotDeathEvent deadBot)
    {
        if (enemies.ContainsKey(deadBot.VictimId))
        {
            enemies.Remove(deadBot.VictimId);
        }
        Rescan();
    }

    private double RelativeEnemyAngle(double BotX, double BotY, bool MoveGun = false)
    {
        double deltaX = BotX - X;
        double deltaY = BotY - Y;
        double headingRadians = Math.Atan2(deltaY, deltaX);
        double headingAngle = headingRadians * RADIANS_TO_DEGREE;
        double deltaAngle;

        if (MoveGun)
        {
            deltaAngle = headingAngle - GunDirection;
            return deltaAngle;
        }
            
        deltaAngle = headingAngle - Direction;
        return deltaAngle;
    }

    private EnemyInfo FindTarget()
    {
        double bestValue = 0;
        EnemyInfo bestTarget = null;
        foreach (var enemy in enemies.Values)
        {
            double distance = DistanceTo(enemy.XPosition, enemy.YPosition);
            double score = (DISTANCE_WEIGHT / distance) + (ENERGY_WEIGHT / enemy.Energy);
            if (score > bestValue)
            {
                bestValue = score;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }
}

class EnemyInfo
{
    public int Id { get; set; }
    public double XPosition { get; set; }
    public double YPosition { get; set; }
    public double Energy { get; set; }
    public double Direction { get; set; }
    public double Speed { get; set; }
    public double Erraticness { get; set; } = 0;
}

class FullRadarScan : Condition
{
    private ATSBot bot;
    private double previousRadarDirection;
    private double cumulativeRotation;

    public FullRadarScan(ATSBot bot)
    {
        this.bot = bot;
        previousRadarDirection = bot.RadarDirection;
        cumulativeRotation = 0;
    }

    public override bool Test()
    {
        double delta = NormalizeRelativeAngle(bot.RadarDirection - previousRadarDirection);
        cumulativeRotation += Math.Abs(delta);
        previousRadarDirection = bot.RadarDirection;

        if (cumulativeRotation >= 360)
        {
            cumulativeRotation = 0;
            return true;
        }
        return false;
    }

    private double NormalizeRelativeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}