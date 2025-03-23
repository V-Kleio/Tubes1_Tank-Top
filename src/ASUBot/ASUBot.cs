using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ----------------------------------------------------------------------------------
// ASU AKA Active Stealth Unit
// ----------------------------------------------------------------------------------
// This bot is designed to operate stealthily on the battlefield. It continuously 
// scans for enemy bots and builds a “danger map” of the arena by dividing it into a
// grid. Each grid cell accumulates a danger score based on two primary factors:
// Enemy Presence and Hit by Bullet. This bot then identifies “safe” regions—areas 
// with the lowest danger score. The bot also adds a random offset to its chosen 
// destination to make its movement more unpredictable, further enhancing its stealth 
// and evasiveness.
// ----------------------------------------------------------------------------------
// To determine safe area, the bot divides the map into a 5x5 grid. While the bot is
// scanning continuously, the bot adds danger score to the grid that the enemy 
// currently on based on the enemy energy value. After calculating all the grid 
// danger score, it choose the lowest danger score grid and move to a random
// coordinate of that grid. There is also a permanent danger score cause by bullet.
// When the bot is hit by a bullet, it marked the grid that correspond to the bot
// position when being hit and add a permanent danger score.
// ----------------------------------------------------------------------------------

public class ASUBot : Bot 
{
    private Dictionary<int, EnemyInfo> enemies = new Dictionary<int, EnemyInfo>();
    private const double RADIANS_TO_DEGREE = 180.0 / Math.PI;
    private const double DEGREE_TO_RADIANS = Math.PI / 180.0;
    private const double BULLET_DANGER_LEVEL = 20;
    private const double FIREPOWER = 3.0;
    private const int GUN_ROTATION_SPEED = 15;
    private const int SPEED = 5;
    private const int GRID_ROWS = 5;
    private const int GRID_COLS = 5;
    private double[,] dangerGrid = new double[GRID_ROWS, GRID_COLS];
    private float cellWidth;
    private float cellHeight;
    private Random randomGenerator = new Random();

    static void Main(string[] args)
    {
        new ASUBot().Start();
    }

    ASUBot() : base(BotInfo.FromFile("ASUBot.json")) { }

    public override void Run()
    {
        cellWidth = ArenaWidth / GRID_COLS;
        cellHeight = ArenaHeight / GRID_ROWS;

        GunTurnRate = GUN_ROTATION_SPEED;
        while (IsRunning)
        {
            PointF safePoint = FindSafestCoordinate();
            MoveTo(safePoint.X, safePoint.Y);
        }
    }

    public override void OnScannedBot(ScannedBotEvent scannedBot)
    {
        Fire(FIREPOWER);
        int id = scannedBot.ScannedBotId;
        EnemyInfo enemy;
        if (!enemies.TryGetValue(id, out enemy))
        {
            enemy = new EnemyInfo();
        }
        enemy.Id = id;
        enemy.XPosition = scannedBot.X;
        enemy.YPosition = scannedBot.Y;
        enemy.Energy = scannedBot.Energy;
        enemy.Direction = scannedBot.Direction;
        enemy.Speed = scannedBot.Speed;
        enemy.Distance = DistanceTo(enemy.XPosition, enemy.YPosition);
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

    public override void OnHitByBullet(HitByBulletEvent bullet)
    {
        int cellX = Math.Min((int)(X / cellWidth), GRID_COLS - 1);
        int cellY = Math.Min((int)(Y / cellHeight), GRID_ROWS - 1);

        dangerGrid[cellY, cellX] += BULLET_DANGER_LEVEL;
    }

    private PointF FindSafestCoordinate()
    {
        double[,] gridDanger = new double[GRID_ROWS, GRID_COLS];
        for (int i = 0; i < GRID_ROWS; i++)
        {
            for (int j = 0; j < GRID_COLS; j++)
            {
                gridDanger[i, j] = dangerGrid[i, j];
            }
        }

        foreach (var enemy in enemies.Values)
        {
            int cellX = Math.Min((int)(enemy.XPosition / cellWidth), GRID_COLS - 1);
            int cellY = Math.Min((int)(enemy.YPosition / cellHeight), GRID_ROWS - 1);
            gridDanger[cellY, cellX] += enemy.Energy;
        }

        double minDanger = double.MaxValue;
        int bestRow = 0;
        int bestCol = 0;
        for (int i = 0; i < GRID_ROWS; i++)
        {
            for (int j = 0; j < GRID_COLS; j++)
            {
                if (gridDanger[i, j] < minDanger)
                {
                    minDanger = gridDanger[i, j];
                    bestRow = i;
                    bestCol = j;
                }
            }
        }

        float safeX = bestCol * cellWidth + cellWidth / 2;
        float safeY = bestRow * cellHeight + cellHeight / 2;

        safeX = safeX + (float)(randomGenerator.NextDouble() * cellWidth / 2 - cellWidth / 4);
        safeY = safeY + (float)(randomGenerator.NextDouble() * cellHeight / 2 - cellHeight / 4);

        return new PointF(safeX, safeY);
    }

    private void MoveTo(double x, double y)
    {
        double angleToEnemy = RelativeEnemyAngle(x, y);
        TurnLeft(NormalizeRelativeAngle(angleToEnemy));
        TurnRate = randomGenerator.NextDouble() * 6 - 3; // random from -3 to 3
        TargetSpeed = SPEED;
        Forward(DistanceTo(x, y));
    }

    private double RelativeEnemyAngle(double BotX, double BotY)
    {
        double deltaX = BotX - X;
        double deltaY = BotY - Y;
        double headingRadians = Math.Atan2(deltaY, deltaX);
        double headingAngle = headingRadians * RADIANS_TO_DEGREE;
        double deltaAngle;
            
        deltaAngle = headingAngle - Direction;
        return deltaAngle;
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
    public double Distance { get; set; }
}