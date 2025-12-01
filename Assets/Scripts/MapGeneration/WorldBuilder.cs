using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.AI.Navigation;
using WorldUtil;
using G = WorldUtil.Geometry;
using C = WorldUtil.Constructs;
using Random = System.Random;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using Unity.Netcode;

public class WorldBuilder : MonoBehaviour
{
    public MeshBuilder originMeshBuilder;
    public GameObject originLandHex;
    public GameObject originWaterHex;
    public BeaverDam originDam;
    public BeaverLodge originLodge;
    public BeaverMound originMound;
    public GameObject originFarmHouse;
    public GameObject originFarmBucket;
    public GameObject treeContainer;
    public GameObject logContainer;

    public GameObject[] logPrefabAssets;
    
    [Header("AI Beaver Spawning")]
    public GameObject aiBeaverPrefab;
    public Vector3[] aiBeaverSpawnPositions = new Vector3[]
    {
        new Vector3(-64.9f, 1.433f, 133.3f),
        new Vector3(-65.7f, 1.433f, 217.9f),
        new Vector3(83f, 1.433f, 133.9f),
        new Vector3(82.4f, 1.433f, 218.8f)
    };

    public NavMeshSurface surface;

    public int mapSize = 7; // longest diameter
    public int forestSize = 6;
    public float waterHeight = 1.9f;
    public float waterSwinDepth = 1f;
    public bool showBuildsOnStart = false;
    public C.Construct construct = C.Construct.HEX7_RIVER6;

    public GameWorld world;


    private void Start()
    {
        // If networking is active, wait for connection before constructing map
        // Otherwise, construct immediately
        if (NetworkManager.Singleton != null)
        {
            // Server constructs when server starts
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            // Clients construct when they connect
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            ConstructMap();
        }
        //surface.BuildNavMesh(); // turned off for now
    }

    private void OnServerStarted()
    {
        ConstructMap();
        
        // Wait a frame for all meshes and colliders to be fully set up before baking NavMesh
        //StartCoroutine(BakeNavMeshAndSpawnBeavers());
    }
    
    private System.Collections.IEnumerator BakeNavMeshAndSpawnBeavers()
    {
        // Wait for end of frame to ensure all geometry is ready
        yield return new WaitForEndOfFrame();
        
        // Check if surface is assigned
        if (surface == null)
        {
            Debug.LogError("WorldBuilder: NavMeshSurface is not assigned! Cannot bake NavMesh.");
            yield break;
        }
        
        Debug.Log("WorldBuilder: Baking NavMesh...");
        surface.BuildNavMesh();
        Debug.Log("WorldBuilder: NavMesh baking complete.");
        
        // Spawn AI beavers after NavMesh is baked (only on server)
        if (aiBeaverPrefab != null && aiBeaverSpawnPositions != null && aiBeaverSpawnPositions.Length > 0)
        {
            for (int i = 0; i < aiBeaverSpawnPositions.Length; i++)
            {
                Vector3 spawnPos = aiBeaverSpawnPositions[i];
                GameObject aiBeaver = Instantiate(aiBeaverPrefab, spawnPos, Quaternion.identity);
                aiBeaver.name = "BeaverAI-" + i;
                Debug.Log($"Spawned AI beaver {i} at position: {spawnPos}");
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only construct on the client itself (not when other clients connect)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            ConstructMap();
            //surface.BuildNavMesh();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    } 


    [ContextMenu("Construct Map")]
    public void ConstructMap()
    {
        // Clean up previous map
        Clear();
        // Setup World instance
        world.Init();
        // Initialize collection of hex/mound objects
        Dictionary<Vector2Int, Hex> hexes = new();
        Dictionary<BeaverMound, List<Vector2Int>> mounds = new();
        List<Vector2> placedMounds = new();
        // Get tree options
        int tcc = treeContainer.transform.childCount;
        GameObject[] originTrees = new GameObject[tcc];
        for (int i = 0; i < tcc; i++)
            originTrees[i] = treeContainer.transform.GetChild(i).gameObject;
        // Get log options

        GameObject[] originLogs = logPrefabAssets;
        // Get hex mesh geometry
        Random rng = new(originMeshBuilder.seed);
        int originalSeed = originMeshBuilder.seed;
        int res = originMeshBuilder.resolution;
        float tPY = treeContainer.transform.position.y;
        float hexW = (res - (res % 2 != 0 ? 1f : 0f)) * originMeshBuilder.scale;
        float sq3 = (float)Math.Sqrt(3f);
        Vector2 hexOff = new(hexW * 0.75f, hexW * 0.25f * sq3);
        Vector2 rowOrg = new(0f, 0f);
        if (mapSize % 2 == 0) mapSize--;
        // instantiate and place all map tiles
        for (int i = 0; i < mapSize; i++)
        {
            int rowLen = mapSize - Math.Abs(i - (mapSize / 2));
            for (int j = 0; j < rowLen; j++)
            {
                int trueJ = i > mapSize / 2 ? (j + (i - (mapSize / 2))) : j;
                Vector2Int truePos = new(i, trueJ);
                originMeshBuilder.seed = rng.Next(999999);
                HexSide uS = G.UpstreamSideOf(truePos, mapSize, construct);
                HexSide dS = G.DownstreamSideOf(truePos, mapSize, construct);
                HexSide[] r = G.RoadSidesOf(truePos, construct);
                bool f = G.IsRiverSource(truePos, construct);
                var meshTuple = originMeshBuilder.GenerateWithFeatures(f, uS, dS, r);
                Mesh lM = meshTuple.Item1;
                Mesh wM = meshTuple.Item2; // Water mesh is generated but wasn't being used
                // copy the reference hex
                GameObject newLandHex = Instantiate(originLandHex, transform);
                GameObject newWaterHex = null;
                
                // Assign the generated mesh to the MeshFilter and MeshCollider
                MeshFilter landMeshFilter = newLandHex.GetComponent<MeshFilter>();
                MeshCollider landMeshCollider = newLandHex.GetComponent<MeshCollider>();
                if (landMeshFilter != null)
                {
                    landMeshFilter.mesh = lM;
                }
                if (landMeshCollider != null)
                {
                    landMeshCollider.sharedMesh = lM;
                }
                
                // place trees on copy
                bool mount = uS == HexSide.NULL && dS == HexSide.NULL;
                if (!mount) BuildTileTrees(newLandHex, originTrees, originLogs,
                    lM, originMeshBuilder.seed, tPY, f);
                // move and name copy
                Vector2 pos = new(
                    rowOrg.x + (j * hexOff.x),
                    rowOrg.y + (j * hexOff.y) + (hexW * 2f)
                );
                newLandHex.transform.position = new(pos.x, 0f, pos.y);
                Vector3 waterPos = new(pos.x, -waterHeight, pos.y);
                
                if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
                {
                    newWaterHex = Instantiate(originWaterHex, transform);
                    newWaterHex.transform.position = waterPos;
                    newWaterHex.name = "Water-" + truePos.x + "-" + truePos.y;
                    
                    MeshFilter waterMeshFilter = newWaterHex.GetComponent<MeshFilter>();
                    MeshCollider waterMeshCollider = newWaterHex.GetComponent<MeshCollider>();
                    if (waterMeshFilter != null)
                    {
                        waterMeshFilter.mesh = wM;
                    }
                    if (waterMeshCollider != null)
                    {
                        waterMeshCollider.sharedMesh = wM;
                    }
                    
                    ForceWaterCollider(newWaterHex);
                    
                    NetworkObject waterNetObj = newWaterHex.GetComponent<NetworkObject>();
                    if (waterNetObj == null)
                    {
                        waterNetObj = newWaterHex.AddComponent<NetworkObject>();
                    }
                    waterNetObj.Spawn();
                }
                else
                {
                    newWaterHex = null;
                }
                
                GameObject[] logs = null;
                if (!mount && (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer))
                {
                    logs = BuildTileLogs(newLandHex, originLogs, lM, originMeshBuilder.seed, tPY, f, new Vector3(pos.x, 0f, pos.y));
                }
                newLandHex.name = "Land-" + truePos.x + "-" + truePos.y;
                BeaverDam bd = null;
                if (dS != HexSide.NULL && (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer))
                {
                    GameObject newDam = Instantiate(
                        originDam.gameObject, transform);
                    Vector2 damPos = G.EquivHexPos(dS, hexW);
                    newDam.transform.position = new(
                        pos.x + damPos.x,
                        waterHeight - originMeshBuilder.hillHeight + 0.2f,
                        pos.y + damPos.y);
                    newDam.transform.localRotation
                        = Quaternion.Euler(0f, G.AngleToAlign(dS), 0f);
                    newDam.name = "Dam-" + truePos.x + "-" + truePos.y;
                    
                    NetworkObject damNetObj = newDam.GetComponent<NetworkObject>();
                    if (damNetObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        damNetObj.Spawn();
                    }
                    else if (damNetObj == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        damNetObj = newDam.AddComponent<NetworkObject>();
                        damNetObj.Spawn();
                    }
                    
                    bd = newDam.GetComponent<BeaverDam>();
                }
                BeaverLodge bl = null;
                if (!mount && !G.IsRiverSource(truePos, construct))
                {
                    GameObject newLodge = Instantiate(
                        originLodge.gameObject, transform);
                    Vector2 cntr = G.EquivHexPos(HexSide.NULL, hexW);
                    newLodge.transform.position = new(
                        pos.x + cntr.x, -waterHeight, pos.y + cntr.y);
                    newLodge.name = "Lodge-" + truePos.x + "-" + truePos.y;
                    newLodge.GetComponent<MeshRenderer>().enabled = showBuildsOnStart;
                    bl = newLodge.GetComponent<BeaverLodge>();
                }
                Hex hex = new(truePos, newLandHex, newWaterHex, bd, bl, logs);
                hexes.Add(truePos, hex);
            }
            rowOrg.x += (i < mapSize / 2) ? -hexOff.x : 0;
            rowOrg.y += (i < mapSize / 2) ? hexOff.y : 2f * hexOff.y;
        }
        originMeshBuilder.seed = originalSeed;
        List<Hex> allHexes = new();
        List<List<Hex>> wRivers = new();
        List<Tuple<Hex, Hex>> wRoads = new();
        List<HexBinding> wMoundLocs = new();
        List<SyrupFarm> wFarms = new();
        int farmCnt = 0;
        foreach (Hex hex in hexes.Values) allHexes.Add(hex);
        foreach (Vector2Int[] river in C.RiverSets(construct))
        {
            List<Hex> hs = new();
            for (int i = 0; i < river.Length; i++)
            {
                Hex targ = hexes[river[i]];
                hs.Add(targ);
                if (i == 0) wFarms.Add(
                    new("Player " + farmCnt++, Color.white, targ));
            }
            wRivers.Add(hs);
        }
        foreach (Vector2Int[] road in C.RoadSets(construct))
        {
            Vector2Int r0 = road[0];
            Vector2Int r1 = road[1];
            Tuple<Hex, Hex> hs = new(hexes[r0], hexes[r1]);
            wRoads.Add(hs);
            HexSide sideBtw = G.EdgeFrom(r0, r1);
            
            if (hexes[r0].waterMesh != null && hexes[r1].waterMesh != null)
            {
                Vector3 r0Pos = hexes[r0].waterMesh
                    .GetComponent<MeshRenderer>().bounds.center;
                Vector3 r1Pos = hexes[r1].waterMesh
                    .GetComponent<MeshRenderer>().bounds.center;
                Vector3 rp = (r0Pos + r1Pos) * 0.5f;
                GameObject newMound = Instantiate(originMound.gameObject, transform);
                newMound.transform.position = new(rp.x, 0f, rp.z);
                newMound.transform.localRotation
                    = Quaternion.Euler(0f, G.AngleToAlign(sideBtw), 0f);
                newMound.name = "Mound-"
                    + r0.x + "-" + r0.y + "-" + r1.x + "-" + r1.y;
                newMound.GetComponent<MeshRenderer>().enabled = showBuildsOnStart;
                BeaverMound bm = newMound.GetComponent<BeaverMound>();
                wMoundLocs.Add(new(hs, bm));
            }
            else
            {
                Debug.LogWarning($"Cannot create mound: waterMesh is null for hexes at {r0} or {r1}");
            }
        }
        GameWorld.Instance().SetWaterHeight(waterHeight);
        GameWorld.Instance().AddWorld(
            new(allHexes, wRivers, wRoads, wMoundLocs, wFarms));
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(MatchWaterNetworkObjectsToHexes(allHexes));
        }
        
        Debug.Log("Made a world with " + wRivers.Count + " rivers and "
            + wRoads.Count + " roads");
    }
    
    private System.Collections.IEnumerator MatchWaterNetworkObjectsToHexes(List<Hex> allHexes)
    {
        yield return new WaitForSeconds(0.5f);
        
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            yield break;
        
        Debug.Log($"[WorldBuilder] Matching NetworkObject water hexes to Hex objects...");
        int matchedCount = 0;
        
        foreach (Hex hex in allHexes)
        {
            if (hex.waterMesh != null)
                continue;
            
            Vector3 expectedWaterPos = new Vector3(
                hex.landMesh.transform.position.x,
                -waterHeight,
                hex.landMesh.transform.position.z
            );
            
            GameObject bestMatch = null;
            float bestDistance = float.MaxValue;
            
            foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (spawnedObj.gameObject.name.StartsWith("Water"))
                {
                    MeshRenderer mr = spawnedObj.gameObject.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        float distance = Vector3.Distance(spawnedObj.gameObject.transform.position, expectedWaterPos);
                        if (distance < 1.0f && distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestMatch = spawnedObj.gameObject;
                        }
                    }
                }
            }
            
            if (bestMatch != null)
            {
                hex.waterMesh = bestMatch;
                
                Vector2Int mapPos = hex.mapPosition;
                HexSide uS = G.UpstreamSideOf(mapPos, mapSize, construct);
                HexSide dS = G.DownstreamSideOf(mapPos, mapSize, construct);
                HexSide[] r = G.RoadSidesOf(mapPos, construct);
                bool f = G.IsRiverSource(mapPos, construct);
                
                var meshTuple = originMeshBuilder.GenerateWithFeatures(f, uS, dS, r);
                Mesh wM = meshTuple.Item2;
                
                MeshFilter waterMeshFilter = bestMatch.GetComponent<MeshFilter>();
                MeshCollider waterMeshCollider = bestMatch.GetComponent<MeshCollider>();
                
                if (waterMeshFilter != null)
                {
                    waterMeshFilter.mesh = wM;
                    Debug.Log($"[WorldBuilder] Assigned mesh to MeshFilter for {bestMatch.name}");
                }
                else
                {
                    Debug.LogWarning($"[WorldBuilder] MeshFilter not found on {bestMatch.name}");
                }
                
                if (waterMeshCollider != null)
                {
                    waterMeshCollider.sharedMesh = wM;
                }
                
                MeshRenderer mr = bestMatch.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = true;
                    
                    MeshRenderer originMR = originWaterHex.GetComponent<MeshRenderer>();
                    if (originMR != null && originMR.sharedMaterial != null)
                    {
                        mr.sharedMaterial = originMR.sharedMaterial;
                        Debug.Log($"[WorldBuilder] Assigned material {originMR.sharedMaterial.name} to {bestMatch.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[WorldBuilder] Could not get material from origin prefab for {bestMatch.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[WorldBuilder] MeshRenderer not found on {bestMatch.name}");
                }
                
                ForceWaterCollider(bestMatch);
                
                if (waterMeshFilter != null && waterMeshFilter.sharedMesh == null)
                {
                    Debug.LogError($"[WorldBuilder] WARNING: Mesh not assigned to {bestMatch.name} after assignment!");
                }
                
                matchedCount++;
                Debug.Log($"[WorldBuilder] Matched and configured water NetworkObject {bestMatch.name} to hex at {hex.mapPosition} (distance: {bestDistance:F3})");
            }
            else
            {
                Debug.LogWarning($"[WorldBuilder] Could not find NetworkObject water hex for hex at {hex.mapPosition}");
            }
        }
        
        Debug.Log($"[WorldBuilder] Matched {matchedCount} water NetworkObjects to hexes");
    }
    
    private void SpawnAIBeavers()
    {
        if (aiBeaverPrefab == null)
        {
            Debug.LogWarning("WorldBuilder: AI Beaver prefab not assigned, skipping AI beaver spawn.");
            return;
        }
        
        if (aiBeaverSpawnPositions == null || aiBeaverSpawnPositions.Length == 0)
        {
            Debug.LogWarning("WorldBuilder: No AI beaver spawn positions defined.");
            return;
        }
        
        for (int i = 0; i < aiBeaverSpawnPositions.Length; i++)
        {
            Vector3 spawnPos = aiBeaverSpawnPositions[i];
            
            GameObject aiBeaver = Instantiate(aiBeaverPrefab, spawnPos, Quaternion.identity);
            aiBeaver.name = "AIBeaver-" + i;
            
            Debug.Log($"Spawned AI beaver {i} at position: {spawnPos}");
        }
    }


    [ContextMenu("Clear")]
    public void Clear()
    {
        List<Transform> children = new();
        for (int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        foreach (Transform t in children) DestroyImmediate(t.gameObject);
    }


    public void ForceWaterCollider(GameObject waterHex)
    {
        MeshCollider mc = waterHex.GetComponent<MeshCollider>();
        MeshFilter mf = waterHex.GetComponent<MeshFilter>();

        // Ensure collider uses its own mesh
        if (mc.sharedMesh == mf.sharedMesh)
            mc.sharedMesh = Instantiate(mf.sharedMesh);

        Mesh wMesh = mc.sharedMesh;

        Vector3[] wVerts = wMesh.vertices;
        for (int i = 0; i < wVerts.Length; i++)
            wVerts[i].y -= waterSwinDepth;

        wMesh.vertices = wVerts;
        wMesh.RecalculateNormals();
        wMesh.RecalculateBounds();
        mc.sharedMesh = wMesh;
    }


    private GameObject[] BuildTileLogs(
        GameObject tile, GameObject[] logs,
        Mesh landMesh, int randSeed, float buildHeight, bool isFarm, Vector3 tileWorldPos)
    {
        Random rng = new(randSeed);
        // Get list of placed trees for reference (needed for log naming)
        List<Vector3> placedTrees = new();
        foreach (Vector3 v in landMesh.vertices)
        {
            if (v.y != 0) continue;
            Vector3 placeAt = new(v.x, buildHeight, v.z);
            if (placedTrees.Contains(placeAt)) continue;
            placedTrees.Add(placeAt);
        }
        
        // List to collect created log GameObjects
        List<GameObject> createdLogs = new List<GameObject>();
        
        // internal logs
        List<Vector3> shufLocations = new(landMesh.vertices);
        Shuffle(shufLocations, rng);
        int logCnt = 0;
        int maxLogs = isFarm ? 0 : forestSize;
        foreach (Vector3 v in shufLocations)
        {
            float lowest = -(originMeshBuilder.hillHeight - waterHeight);
            if (v.y < lowest || v.y > lowest / 4f) continue;
            if (logCnt++ >= maxLogs) break;
            // Calculate position in local space relative to tile
            Vector3 localPos = new(v.x, v.y + buildHeight, v.z);
            // Convert to world space by adding tile's world position
            Vector3 worldPos = tileWorldPos + localPos;
            
            GameObject logPrefab = logs[rng.Next(logs.Length)];
            GameObject log = Instantiate(logPrefab, null);
            log.transform.position = worldPos;
            log.name = "Log" + placedTrees.Count;
            
            // Add to list of created logs
            createdLogs.Add(log);
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkObject logNetObj = log.GetComponent<NetworkObject>();
                if (logNetObj == null)
                {
                    // Add NetworkObject at runtime if prefab doesn't have one
                    logNetObj = log.AddComponent<NetworkObject>();
                }
                logNetObj.Spawn();
            }
        }
        return createdLogs.ToArray();
    }

    private void BuildTileTrees(
        GameObject tile, GameObject[] trees, GameObject[] logs,
        Mesh landMesh, int randSeed, float buildHeight, bool isFarm)
    {
        Random rng = new(randSeed);
        // border trees
        List<Vector3> placedTrees = new();
        foreach (Vector3 v in landMesh.vertices)
        {
            if (v.y != 0) continue;
            Vector3 placeAt = new(v.x, buildHeight, v.z);
            if (placedTrees.Contains(placeAt)) continue;
            GameObject tree = Instantiate(
               trees[rng.Next(trees.Length)], tile.transform);
            tree.transform.position = placeAt;
            tree.name = "Tree" + placedTrees.Count;
            placedTrees.Add(placeAt);
        }
        if (isFarm)
        {
            // place buckets around internal border trees
            List<Vector3> shufLocations = new(landMesh.vertices);
            Shuffle(shufLocations, rng);
            List<Vector2> placedBuckets = new();
            int bCnt = 0;
            int bMax = 24;
            foreach (Vector3 v in shufLocations)
            {
                if (bCnt >= bMax) break;
                Vector3 placeAt = new(v.x, v.y + buildHeight, v.z);
                if (placedBuckets.Contains(placeAt)
                    || placedTrees.Contains(placeAt)) continue;
                bool placeHere = false;
                Vector2 v2d = new(v.x, v.z);
                foreach (Vector3 tv in placedTrees)
                {
                    Vector2 tv2d = new(tv.x, tv.z);
                    if (Vector2.Distance(v2d, tv2d) == originMeshBuilder.scale)
                    {
                        placeHere = true; break;
                    }
                }
                if (!placeHere) continue;
                GameObject b = Instantiate(originFarmBucket, tile.transform);
                b.transform.position = placeAt;
                b.name = "Bucket" + bCnt++;
            }
            // wood house in tile center
            Vector3 centre = landMesh.vertices[landMesh.vertices.Length / 12];
            GameObject house = Instantiate(originFarmHouse, tile.transform);
            house.transform.position = new(centre.x, 0f, centre.z);
            house.transform.localRotation = Quaternion.Euler(0f, rng.Next(360), 0f);
            house.name = "House";
        }
    }


    // stolen from StackOverflow
    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

}