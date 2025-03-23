using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

/*
 * OBI-WAN TANKNOBI v1.0 - "It's over, I have the high ground"
 * Author: Muhammad Kinan Arkansyaddad
 * 
 * Algorithm Overview:
 * ------------------------------
 * This bot takes inspiration from the Jedi Master, Obi-Wan Kenobi, incorporating a greedy algorithm focused on maintaining the "high ground" and using reactive defense and offense strategies.
 * 
 * 1. Positioning & Setup (Greedy High Ground Strategy)
 *    - Setup Position: OBI-WAN TANKNOBI quickly positions itself in the top portion of the arena, ensuring it has the "high ground". The bot always seeks to stay above its opponents to maintain superior positioning, mirroring Obi-Wan's philosophy in the iconic battle on Mustafar.
 *    - The greedy nature here lies in prioritizing immediate vertical control of the arena without considering other tactical advantages beyond high ground positioning.
 * 
 * 2. Scanning & Tracking Enemies (Greedy Decision-Making for Targeting)
 *    - OBI-WAN TANKNOBI constantly scans for enemies, updating a dictionary of enemy positions and energy levels. 
 *    - It reacts to enemy energy drops to identify when an enemy has fired.
 *    - This bot doesn't predict enemy movement, instead it reacts quickly based on the current situation.
 * 
 * 3. Dodging Enemy Fire (Greedy Evasive Maneuvering)
 *    - If an enemy is detected firing, OBI-WAN TANKNOBI will perform an evasive maneuver with forward or backward movement.
 *    - The bot avoids standing still, instead opting for the immediate action of moving to avoid damage.
 * 
 * 4. Combat Engagement (Greedy Offensive Strategy)
 *    - Once OBI-WAN TANKNOBI sees the enemy, OBI-WAN TANKNOBI doesn't hesitate to fire.
 *    - The bot acts quickly, opting to shoot whenever possible. However, it does not analyze trajectory, meaning it may miss if the enemy is moving.
 * 
 * 5. Radar & Gun Movement (Greedy Tactical Turns)
 *    - The bot alternates between turning its gun left and right, ensuring it constantly scans the arena for threats.
 *    - This allows for continuous detection of enemies and fast reaction times.
 * 
 * Strengths:
 * ----------
 * - Quick Reflexes: Reacts swiftly to immediate threats, especially with its positioning advantage of the high ground.
 * - Simple Decision-Making: Focuses on quick actions to evade and engage without unnecessary complexity.
 * - Effective in Early Engagements: The bot is particularly effective when positioning in the high ground and controlling the vertical aspect of combat.
 * 
 * Weaknesses:
 * -----------
 * - Lack of Predictive Strategy: OBI-WAN TANKNOBI doesn't predict enemy movement, making its shots less effective against fast or evasive opponents.
 * - Reactive Evasion: Dodging is entirely reactionary, and the bot may not be as effective against bots with more strategic planning.
 * - Energy Inefficiency: No long-term energy conservation strategy, leading to potential exhaustion in longer battles.
 * - Limited Tactical Depth: OBI-WAN TANKNOBI's focus on high ground may limit its ability to adapt in complex or dynamic combat situations.
 */
public class ObiWanTanknobi : Bot
{
    private Dictionary<string, EnemyInfo> enemies = new Dictionary<string, EnemyInfo>();
    private Dictionary<string, double> lastEnergy = new Dictionary<string, double>();
    private string enemyShot;
    private bool turnRadarLeft = false;

    public static void Main(string[] args) => new ObiWanTanknobi().Start();


    public ObiWanTanknobi() : base(BotInfo.FromFile("ObiWanTanknobi.json")) { }

    public override void OnRoundStarted(RoundStartedEvent e)
    {
        enemies.Clear();
        lastEnergy.Clear();
        enemyShot = "";
        turnRadarLeft = false;
    }

    public override void Run()
    {
        while (IsRunning)
        {
            Strategy();
        }
    }

    private void Strategy()
    {
        // Setup tank position to top part of the arena
        SetupPosition();

        // Detecting any change in enemies' energy
        if (ShouldDodge())
            // if there's changes, evade because likely it's a shot
            Evade(enemyShot);
    }

    private void SetupPosition()
    {
        if (Y > ArenaHeight * 0.9 && Direction == 0)
        {
            return;
        }
        double targetY = ArenaHeight;
        double currentBearing = NormalizeBearing(Direction);

        if (currentBearing != 90)
        {
            SetTurnLeft(NormalizeBearing(90 - currentBearing));
            WaitFor(new TurnCompleteCondition(this));
        }

        if (Y < targetY)
        {
            SetForward(targetY - Y);
            WaitFor(new MoveCompleteCondition(this));
        }

        double targetAngle = 0;

        if (NormalizeBearing(Direction) != targetAngle)
        {
            SetTurnLeft(NormalizeBearing(targetAngle - Direction));
            WaitFor(new TurnCompleteCondition(this));
        }
    }

    private bool ShouldDodge()
    {
        if (turnRadarLeft)
        {
            TurnGunLeft(180);
            turnRadarLeft = false;
        }
        else
        {
            TurnGunRight(180);
            turnRadarLeft = true;
        }
        WaitFor(new RadarTurnCompleteCondition(this));

        foreach (var enemy in enemies)
        {
            string enemyId = enemy.Key;
            if (lastEnergy.ContainsKey(enemyId) && enemies.ContainsKey(enemyId))
            {
                if (enemies[enemyId].Energy < lastEnergy[enemyId])
                {
                    enemyShot = enemyId;
                    return true;
                }
            }
        }
        return false;
    }

    private void Evade(string enemyId)
    {
        if (!enemies.ContainsKey(enemyId) || !lastEnergy.ContainsKey(enemyId))
            return;

        if (X < ArenaWidth / 2)
        {
            SetForward(300);
        }
        else
        {
            SetBack(300);
        }

        // WaitFor(new MoveCompleteCondition(this));
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        Fire(3);
        string enemyId = e.ScannedBotId.ToString();
        if (enemies.ContainsKey(enemyId))
        {
            lastEnergy[enemyId] = enemies[enemyId].Energy;
        }

        enemies[enemyId] = new EnemyInfo(e.X, e.Y, e.Energy);
    }
    private double GetDistance(double x, double y)
    {
        return Math.Sqrt(Math.Pow(x - X, 2) + Math.Pow(y - Y, 2));
    }

    private double NormalizeBearing(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private class EnemyInfo
    {
        public double X { get; }
        public double Y { get; }
        public double Energy { get; }

        public EnemyInfo(double x, double y, double energy)
        {
            X = x;
            Y = y;
            Energy = energy;
        }
    }
}

public class TurnCompleteCondition : Condition
{
    private readonly Bot bot;

    public TurnCompleteCondition(Bot bot)
    {
        this.bot = bot;
    }

    public override bool Test()
    {
        return bot.TurnRemaining == 0;
    }
}

public class RadarTurnCompleteCondition : Condition
{
    private readonly Bot bot;

    public RadarTurnCompleteCondition(Bot bot)
    {
        this.bot = bot;
    }

    public override bool Test()
    {
        return bot.RadarTurnRemaining == 0;
    }
}

public class MoveCompleteCondition : Condition
{
    private readonly Bot bot;

    public MoveCompleteCondition(Bot bot)
    {
        this.bot = bot;
    }

    public override bool Test()
    {
        return bot.DistanceRemaining == 0;
    }
}