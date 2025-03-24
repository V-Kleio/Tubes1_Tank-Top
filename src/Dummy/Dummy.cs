using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// ---------------------------------------------
// ATS001A AKA Aggresive Target Seeker
// ---------------------------------------------
// This bot utilizes the strategy to find the
// optimal target. Every turn, scan for enemies 
// and immediately target the one that is 
// closest and lowest on energy. Fire powerful 
// shots when within range.
// ---------------------------------------------

public class Dummy : Bot 
{
    static void Main(string[] args)
    {
        new Dummy().Start();
    }

    Dummy() : base(BotInfo.FromFile("Dummy.json")) { }

    public override void Run()
    {
    }
}