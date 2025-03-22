using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ---------------------------------------------
// ASS AKA Aggresive Seeker Sharpshooter
// ---------------------------------------------
// This bot utilizes the strategy to find the
// optimal target. Every turn, scan for enemies 
// and immediately target the one that is 
// closest and lowest on energy. Fire powerful 
// shots when within range.
// ---------------------------------------------

public class ATSBot : Bot 
{
    private Dictionary<int, EnemyInfo> enemies = new Dictionary<int, EnemyInfo>();

    static void Main(string[] args)
    {
        new ATSBot().Start();
    }

    ATSBot() : base(BotInfo.FromFile("ATSBot.json")) { }

    public override void Run()
    {
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
            EnemyInfo target = FindTarget();

            if (target != null)
            {
                double distance = DistanceTo(target.XPosition, target.YPosition);
                double firepower = distance > 300 ? 1 : 3;
                

                double bulletSpeed = 20 - 3 * firepower;
                double travelTime = distance / bulletSpeed;

                double enemyRadians = target.Direction * Math.PI / 180.0;
                double predictedX = target.XPosition + target.Speed * Math.Cos(enemyRadians) * travelTime;
                double predictedY = target.YPosition + target.Speed * Math.Sin(enemyRadians) * travelTime;

                TurnToEnemyTank(predictedX, predictedY, true);
                Fire(firepower);

                TurnToEnemyTank(target.XPosition, target.YPosition);

                if (distance > 300)
                {
                    Forward(Math.Min(distance - 250, 100));
                }
                else
                {
                    TurnRight(90);
                    Forward(50);
                }

            }
        }

        TurnRadarRight(360);
    }

    public override void OnScannedBot(ScannedBotEvent scannedBot)
    {
        enemies[scannedBot.ScannedBotId] = new EnemyInfo
        {
            Id = scannedBot.ScannedBotId,
            XPosition = scannedBot.X,
            YPosition = scannedBot.Y,
            Energy = scannedBot.Energy,
            Direction = scannedBot.Direction,
            Speed = scannedBot.Speed
        };
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

    private void TurnToEnemyTank(double BotX, double BotY, bool MoveGun = false)
    {
        double deltaX = BotX - X;
        double deltaY = BotY - Y;
        double headingRadians = Math.Atan2(deltaY, deltaX);
        double headingAngle = headingRadians * 180.0 / Math.PI;

        if (MoveGun)
        {
            double deltaAngle = headingAngle - GunDirection;
            TurnGunLeft(NormalizeRelativeAngle(deltaAngle));
        }
        else
        {
            double deltaAngle = headingAngle - Direction;
            TurnLeft(NormalizeRelativeAngle(deltaAngle));
        }
    }

    private EnemyInfo FindTarget()
    {
        double bestValue = 0;
        EnemyInfo bestTarget = null;
        foreach (var enemy in enemies.Values)
        {
            double distance = DistanceTo(enemy.XPosition, enemy.YPosition);
            double score = (1.0 / distance) + (5.0 / enemy.Energy);
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