using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using WorldUtil;
using Unity.Netcode;

public class BoatSpawner : MonoBehaviour
{
    // Make sure the BoatSpawner object is at exactly {0,0,0}
    // Otherwise they won't know where the rivers are

    public PreviewBoat originBoat;
    public int spawnInterval = 256;
    public bool spawnerOn = false;

    private int frameCount = 0;

    private readonly Vector3 spawnOffset = new(0.1f, 0.0f, 0.1f);

    private Dictionary<SyrupFarm, List<List<Hex>>> routes = new();


    private void FixedUpdate()
    {
        if (!spawnerOn) return;
        if (frameCount++ % spawnInterval != 0) return;
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

        // Safety assurance
        if (GameWorld.Instance() == null) return;
        if (GameWorld.Instance().World() == null) return;

        // Summon a PreviewBoat at each syrup farm location
        // The boat will autonomously travel downstream
        //   so from here we can just let it do its thing
        foreach (SyrupFarm f in GameWorld.Instance().World().syrupFarms)
        {
            // Decide target path for boat
            List<List<Hex>> possRoutes = routes[f];
            if (routes[f].Count == 0) continue;
            // Create actual boat objects
            // One boat per route to go
            foreach (List<Hex> path in routes[f])
            {
                Vector3 spawnPoint = f.location.waterMesh
                    .GetComponent<MeshRenderer>().bounds.center;
                GameObject boat = Instantiate(originBoat.gameObject, transform);
                boat.transform.position = spawnPoint + spawnOffset;
                boat.name = "Boat";
                
                if (NetworkManager.Singleton != null)
                {
                    NetworkObject boatNetObj = boat.GetComponent<NetworkObject>();
                    if (boatNetObj == null)
                    {
                        boatNetObj = boat.AddComponent<NetworkObject>();
                    }
                    boatNetObj.Spawn();
                }
                
                boat.GetComponent<PreviewBoat>().SetRoute(path);
            }
        }
    }

    public void RefreshRoutes()
    {
        // This first part is here because the world isn't finished
        //  generating until midway thru Start() execution. So we will instead
        //  fill the turn counter during the first refresh which occurs then
        routes.Clear();
        foreach (SyrupFarm f in GameWorld.Instance().World().syrupFarms)
        {
            Dictionary<Hex, List<Hex>> tps = GameWorld.Instance()
                .leaderboard.GetTradePathsFor(f);
            List<List<Hex>> raw = new();
            foreach (List<Hex> r in tps.Values) raw.Add(r);
            routes.Add(f, raw);
        }
    }
}
