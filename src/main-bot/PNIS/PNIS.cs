using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

/*
 * PNIS v1.0 - "Predictive enemy Neutralization & Intelligent Shooting"
 * Author: Muhammad Kinan Arkansyaddad
 * 
 * Algorithm Overview:
 * ------------------------------
 * PNIS is a bot designed to balance precise targeting, adaptive movement, and effective evasion. 
 * It integrates a mix of prediction-based shooting, strafing maneuvers, and radar tracking to outmaneuver opponents.
 * 
 * 1. Enemy Tracking & Radar Control:
 *    - PNIS constantly scans for enemies and keeps track of their position, energy, speed, and direction in a dictionary.
 *    - The radar is adjusted dynamically to maximize scanning coverage and quickly re-acquire lost targets.
 *    - Once an enemy is detected, the bot adjusts its radar to lock onto the enemy and optimize continuous tracking.
 * 
 * 2. Smart Targeting & Predictive Firing:
 *    - The bot calculates an estimated future position of the enemy based on their movement speed and direction.
 *    - It adjusts its gun bearing to aim at the predicted position rather than the enemy's current location.
 *    - Firepower is dynamically chosen based on distance, using:
 *      - High firepower (3) for close-range fights.
 *      - Medium firepower (2) for mid-range engagements.
 *      - Low firepower (1) for long-distance sniping.
 * 
 * 3. Adaptive Movement:
 *    - PNIS constantly moves to avoid being an easy target.
 *    - If the enemy is far away, it makes larger movements to close the gap strategically.
 *    - If the enemy is too close, it strafes unpredictably to make aiming more difficult for opponents.
 *    - Mid-range encounters involve switching between defensive and offensive positioning.
 * 
 * 4. Strafing & Evasion:
 *    - PNIS uses a continuous strafing strategy to avoid linear movement patterns that could be exploited.
 *    - If it collides with a wall or another bot, it immediately reverses direction.
 *    - Randomized strafing direction changes help it dodge enemy fire.
 * 
 * 5. Reactive Countermeasures:
 *    - If PNIS detects an enemy nearby, it dynamically adjusts its movement strategy.
 *    - The bot's evasive maneuvers become more aggressive when in close proximity.
 *    - Instead of fleeing outright, it maintains a balance between engaging and dodging.
 * 
 * Strengths:
 * ----------
 * - Predictive Aiming: Uses forward trajectory calculations for improved accuracy.
 * - Continuous Radar Coverage: Minimizes enemy downtime in the bot's field of view.
 * - Adaptive Movement: Adjusts movement strategy based on enemy distance and engagement rules.
 * - Strafe Evasion: Constant motion makes PNIS harder to hit in sustained battles.
 * 
 * Weaknesses:
 * -----------
 * - Prediction Errors: If an enemy frequently changes speed or direction, shots may miss.
 * - Randomized Movement: Occasionally makes inefficient movement choices due to randomized elements.
 * - Energy Consumption: Continuous movement and firing may drain energy faster in prolonged fights.
 * - Close-Quarters Vulnerability: Can struggle against aggressive melee-style bots with instant-reaction attacks.
 * 
 */

public class PNIS : Bot
{
    private Dictionary<string, EnemyInfo> enemies = new Dictionary<string, EnemyInfo>();

    public PNIS() : base(BotInfo.FromFile("PNIS.json")) { }

    private int moveForward = 1;

    public override void Run()
    {
        TurretColor = Color.Black;
        ScanColor = Color.Black;
        BulletColor = Color.Black;
        BodyColor = Color.Black;
        RadarColor = Color.Black;
        TracksColor = Color.Black;
        GunColor = Color.Black;

        AdjustRadarForGunTurn = true;
        TurnRadarRight(360);
        while (IsRunning)
        {
            Strafe();
            if (RadarTurnRemaining == 0)
                TurnRadarRight(360);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        var dist = DistanceTo(e.X, e.Y);

        var bearing = BearingTo(e.X, e.Y);
        var gunBearing = GunBearingTo(e.X, e.Y);

        SetTurnLeft(bearing + 90);
        SetTurnGunLeft(gunBearing);

        RotateRadar(e.X, e.Y);

        string enemyId = e.ScannedBotId.ToString();
        enemies[enemyId] = new EnemyInfo(e.X, e.Y, e.Energy, e.Speed, e.Direction);

        SmartFire(enemies[enemyId]);
        AdaptiveMovement(enemies[enemyId]);
    }

    private void RotateRadar(double x, double y)
    {
        double radarbearing = RadarBearingTo(x, y);
        double margin = 5;
        if (radarbearing > 0)
        {
            SetTurnRadarLeft(radarbearing + margin);
        }
        else
        {
            SetTurnRadarRight(-radarbearing + margin);
        }
    }

    private void SmartFire(EnemyInfo e)
    {
        double bulletSpeed = 20 - (3 * 2);
        double distance = DistanceTo(e.X, e.Y);
        double enemySpeed = e.Speed;
        double enemyDirection = e.Direction;

        double timeToHit = distance / bulletSpeed;

        double predictedX = e.X + Math.Cos(Math.PI * enemyDirection / 180) * enemySpeed * timeToHit;
        double predictedY = e.Y + Math.Sin(Math.PI * enemyDirection / 180) * enemySpeed * timeToHit;

        double gunBearing = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(gunBearing);

        double firePower = distance < 200 ? 3 : distance < 500 ? 2 : 1;
        SetFire(firePower);

        // Debugging prints
        // Console.WriteLine($"--- SmartFire Debug ---");
        // Console.WriteLine($"Enemy Position: ({e.X}, {e.Y})");
        // Console.WriteLine($"Predicted Position: ({predictedX}, {predictedY})");
        // Console.WriteLine($"Distance: {distance}");
        // Console.WriteLine($"Enemy Speed: {enemySpeed}");
        // Console.WriteLine($"Enemy Direction: {enemyDirection}");
        // Console.WriteLine($"Time to Hit: {timeToHit}");
        // Console.WriteLine($"Gun Bearing Adjustment: {gunBearing}");
        // Console.WriteLine($"Fire Power: {firePower}");
        // Console.WriteLine($"------------------------");
    }

    private void AdaptiveMovement(EnemyInfo e)
    {
        double distance = DistanceTo(e.X, e.Y);
        double enemyBearing = BearingTo(e.X, e.Y);

        if (distance > 500)
        {
            SetTurnLeft(enemyBearing + (new Random().NextDouble() > 0.5 ? 30 : -30));
            SetForward(200);
        }
        else if (distance < 100)
        {
            moveForward = new Random().NextDouble() > 0.5 ? 1 : -1;
            SetTurnLeft(enemyBearing + 90 * moveForward);
            SetForward(150 * moveForward);
        }
        else
        {
            // Mid-range
            if (new Random().NextDouble() > 0.7)
            {
                moveForward *= -1;
            }
            SetTurnLeft(enemyBearing + 90);
            SetForward(100 * moveForward);
        }
    }

    private void Strafe()
    {
        SetForward(10000 * moveForward);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        moveForward *= -1;
    }

    public override void OnHitBot(HitBotEvent e)
    {
        moveForward *= -1;
    }

    public static void Main(string[] args) => new PNIS().Start();
    private class EnemyInfo
    {
        public double X { get; }
        public double Y { get; }
        public double Energy { get; }
        public double Speed { get; }
        public double Direction { get; }

        public EnemyInfo(double x, double y, double energy, double speed, double direction)
        {
            X = x;
            Y = y;
            Energy = energy;
            Speed = speed;
            Direction = direction;
        }
    }
}

