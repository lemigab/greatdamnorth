using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.SocialPlatforms.Impl;
using WorldUtil;

public class TradeLeaderboard : MonoBehaviour
{
    public BoatSpawner boatSpawner;

    private readonly Dictionary<Hex, SyrupFarm> tradeControl = new();
    private readonly Dictionary<SyrupFarm, int> scores = new();

    private readonly Dictionary<SyrupFarm, 
        Dictionary<Hex, List<Hex>>> tradePaths = new();

    // Each farm gains this much score every round per controlled tile
    // A farm 'controls' a tile by having a trade route through it
    public int scorePerTile = 1;

    public int scoreCountInterval = 256;
    public bool scoreCounterOn = false;

    private int frameCount = 0;

    private void FixedUpdate()
    {
        if (!scoreCounterOn) return;
        if (frameCount++ % scoreCountInterval != 0) return;

        GrantScores();
    }

    private void GrantScores()
    {
        foreach (Hex hex in tradeControl.Keys)
        {
            if (tradeControl[hex] != null)
                scores[tradeControl[hex]] += scorePerTile;
        }
    }

    // Completely re-calculates for all hexes
    public void RefreshTradeControl()
    {
        // Reset everything
        tradeControl.Clear();
        tradePaths.Clear();
        World world = GameWorld.Instance().World();
        foreach (Hex hex in world.all) tradeControl.Add(hex, null);
        // Iterate all farms, none should have overlapping routes
        foreach (SyrupFarm farm in scores.Keys)
        {
            tradePaths[farm] = world.AllTradePathsFor(farm);
            foreach (List<Hex> route in tradePaths[farm].Values)
                foreach (Hex h in route) tradeControl[h] = farm;
        }
        // Remind boat spawner
        boatSpawner.RefreshRoutes();
    }

    public Dictionary<Hex, List<Hex>> GetTradePathsFor(
        SyrupFarm farm) => tradePaths[farm];

    public SyrupFarm GetTradeControl(Hex hex) => tradeControl[hex];

    public string ScoreSheet()
    {
        string sheet = "Trade Leaderboard Scores";
        foreach (SyrupFarm f in scores.Keys)
            sheet += "\n" + f.name + " : " + scores[f];
        return sheet;
    }

    public string TradeSheet()
    {
        string sheet = "Trade Control Per Hex";
        foreach (Hex h in tradeControl.Keys)
            sheet += "\n" + h.mapPosition + " : " + tradeControl[h]?.name;
        return sheet;
    }

    public void ClearAll()
    {
        tradeControl.Clear();
        scores.Clear();
    }

    public void Init(World world)
    {
        foreach (SyrupFarm farm in world.syrupFarms) scores.Add(farm, 0);
        RefreshTradeControl();
    }


    [ContextMenu("Print Out Scores")]
    public void PrintScores() => Debug.Log(ScoreSheet());


    [ContextMenu("Print Out Trade Data")]
    public void PrintData() => Debug.Log(TradeSheet());


    [ContextMenu("Award Trade Scores")]
    public void Score() => GrantScores();

}