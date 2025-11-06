using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WorldUtil;
using G = WorldUtil.Geometry;
using C = WorldUtil.Constructs;
using Random = System.Random;

public class WorldBuilder : MonoBehaviour
{
    public MeshBuilder originMeshBuilder;
    public GameObject originLandHex;
    public GameObject originWaterHex;
    public BeaverDam originDam;
    public GameObject originLog;
    public GameObject treeContainer;

    public int mapSize; // longest diameter
    public float waterHeight;
    public C.Construct construct;


    private void Start()
    {
        ConstructMap();
    }


    [ContextMenu("Construct Map")]
    public void ConstructMap()
    {
        // Clean up previous map
        Clear();
        // Initialize collection of hex objects
        Dictionary<Vector2Int, Hex> hexes = new();
        // Get tree options
        int cc = treeContainer.transform.childCount;
        GameObject[] originTrees = new GameObject[cc];
        for (int i = 0; i < cc; i++)
            originTrees[i] = treeContainer.transform.GetChild(i).gameObject;
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
                Mesh lM = originMeshBuilder.GenerateWithFeatures(uS, dS, r);
                // copy the reference hex
                GameObject newLandHex = Instantiate(originLandHex, transform);
                GameObject newWaterHex = Instantiate(originWaterHex, transform);
                // place trees on copy
                bool mount = uS == HexSide.NULL && dS == HexSide.NULL;
                if (!mount)
                {
                    // chewable log (temporary for A3)
                    GameObject log = Instantiate(originLog, newLandHex.transform);
                    log.name = "Log";
                    // border trees
                    List<Vector3> placedTrees = new();
                    foreach (Vector3 v in lM.vertices)
                    {
                        if (v.y != 0) continue;
                        Vector3 placeAt = new(v.x, tPY, v.z);
                        if (placedTrees.Contains(placeAt)) continue;
                        GameObject tree = Instantiate(
                           originTrees[rng.Next(cc)], newLandHex.transform);
                        tree.transform.position = placeAt;
                        tree.name = "Tree" + placedTrees.Count;
                        placedTrees.Add(placeAt);
                    }
                }
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
                    BeaverDam bd = newDam.GetComponent<BeaverDam>();
                    Hex hex = new(truePos, newLandHex, newWaterHex, bd);
                    hexes.Add(truePos, hex);
                }
                else
                {
                    Hex hex = new(truePos, newLandHex, newWaterHex, null);
                    hexes.Add(truePos, hex);
                }
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
        foreach (Hex hex in hexes.Values) allHexes.Add(hex);
        foreach (Vector2Int[] river in C.RiverSets(construct))
        {
            List<Hex> hs = new();
            foreach (Vector2Int v in river)hs.Add(hexes[v]); 
            wRivers.Add(hs);
        }
        foreach (Vector2Int[] road in C.RoadSets(construct))
        {
            Tuple<Hex, Hex> hs = new(hexes[road[0]], hexes[road[1]]);
            wRoads.Add(hs);
        }
        GameWorld.Instance().SetWaterHeight(waterHeight);
        GameWorld.Instance().AddWorld(new(allHexes, wRivers, wRoads));
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

}
