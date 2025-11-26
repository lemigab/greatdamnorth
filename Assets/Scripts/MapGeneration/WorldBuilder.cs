using System;
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

    public NavMeshSurface surface;

    public int mapSize = 7; // longest diameter
    public int forestSize = 6;
    public float waterHeight = 1.9f;
    public C.Construct construct = C.Construct.HEX7_RIVER6;

    public GameWorld world;


    private void Start()
    {
        ConstructMap();
        //surface.BuildNavMesh(); // turned off for now

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
        int lcc = logContainer.transform.childCount;
        GameObject[] originLogs = new GameObject[lcc];
        for (int i = 0; i < lcc; i++)
            originLogs[i] = logContainer.transform.GetChild(i).gameObject;
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
                Mesh lM = originMeshBuilder.GenerateWithFeatures(f, uS, dS, r);
                // copy the reference hex
                GameObject newLandHex = Instantiate(originLandHex, transform);
                GameObject newWaterHex = Instantiate(originWaterHex, transform);
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
                newWaterHex.transform.position = new(pos.x, waterHeight, pos.y);
                newLandHex.name = "Land-" + truePos.x + "-" + truePos.y;
                newWaterHex.name = "Water-" + truePos.x + "-" + truePos.y;
                // build dam if downstream exists
                BeaverDam bd = null;
                if (dS != HexSide.NULL)
                {
                    GameObject newDam = Instantiate(
                        originDam.gameObject, transform);
                    Vector2 damPos = G.EquivHexPos(dS, hexW);
                    newDam.transform.position = new(
                        pos.x + damPos.x,
                        waterHeight - originMeshBuilder.hillHeight,
                        pos.y + damPos.y);
                    newDam.transform.localRotation
                        = Quaternion.Euler(0f, G.AngleToAlign(dS), 0f);
                    newDam.name = "Dam-" + truePos.x + "-" + truePos.y;
                    bd = newDam.GetComponent<BeaverDam>();
                }
                // build lodge if river exists and outside farm
                BeaverLodge bl = null;
                if (!mount && !G.IsRiverSource(truePos, construct))
                {
                    GameObject newLodge = Instantiate(
                        originLodge.gameObject, transform);
                    Vector2 cntr = G.EquivHexPos(HexSide.NULL, hexW);
                    newLodge.transform.position = new(
                        pos.x + cntr.x, -waterHeight, pos.y + cntr.y);
                    newLodge.name = "Lodge-" + truePos.x + "-" + truePos.y;
                    bl = newLodge.GetComponent<BeaverLodge>();
                }
                // Create and store hex object
                Hex hex = new(truePos, newLandHex, newWaterHex, bd, bl);
                hexes.Add(truePos, hex);
            }
            rowOrg.x += (i < mapSize / 2) ? -hexOff.x : 0;
            rowOrg.y += (i < mapSize / 2) ? hexOff.y : 2f * hexOff.y;
        }
        // reset the seed
        originMeshBuilder.seed = originalSeed;
        // build game world
        List<Hex> allHexes = new();
        List<List<Hex>> wRivers = new();
        List<Tuple<Hex, Hex>> wRoads = new();
        List<HexBinding> wMoundLocs = new();
        List<SyrupFarm> wFarms = new();
        int farmCnt = 0;
        foreach (Hex hex in hexes.Values) allHexes.Add(hex);
        foreach (Vector2Int[] river in C.RiverSets(construct))
        {
            // add a farm at river origin
            List<Hex> hs = new();
            for (int i = 0; i < river.Length; i++)
            {
                Hex targ = hexes[river[i]];
                hs.Add(targ);
                if (i == 0) wFarms.Add(
                    new("Farm" + farmCnt++, Color.white, targ));
            }
            wRivers.Add(hs);
        }
        foreach (Vector2Int[] road in C.RoadSets(construct))
        {
            Vector2Int r0 = road[0];
            Vector2Int r1 = road[1];
            // Add road
            Tuple<Hex, Hex> hs = new(hexes[r0], hexes[r1]);
            wRoads.Add(hs);
            // Add mound
            HexSide sideBtw = G.EdgeFrom(r0, r1);
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
            wMoundLocs.Add(new(hs, newMound.GetComponent<BeaverMound>()));
        }
        GameWorld.Instance().SetWaterHeight(waterHeight);
        GameWorld.Instance().AddWorld(
            new(allHexes, wRivers, wRoads, wMoundLocs, wFarms));
        Debug.Log("Made a world with " + wRivers.Count + " rivers and "
            + wRoads.Count + " roads");
    }


    [ContextMenu("Clear")]
    public void Clear()
    {
        List<Transform> children = new();
        for (int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        foreach (Transform t in children) DestroyImmediate(t.gameObject);
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
            Vector3 placeAt = new(v.x, v.y + buildHeight, v.z);
            GameObject log = Instantiate(
               logs[rng.Next(logs.Length)], tile.transform);
            log.transform.position = placeAt;
            log.name = "Log" + placedTrees.Count;
        }
        if (isFarm)
        {
            // place buckets around internal border trees
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
