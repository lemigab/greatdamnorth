using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WorldUtil;
using C = WorldUtil.Constructs;
using Random = System.Random;

public class WorldBuilder : MonoBehaviour
{
    public MeshBuilder originMeshBuilder;
    public GameObject originLandHex;
    public GameObject originWaterHex;
    public BeaverDam originDam;

    public int mapSize; // longest diameter
    public float waterHeight;
    public Constructs.Construct construct;


    [ContextMenu("Construct Map")]
    public void ConstructMap()
    {
        // Clean up previous map
        Clear();
        // Initialize collection of hex objects
        Dictionary<Vector2Int, Hex> hexes = new();
        // Get hex mesh geometry
        Random rng = new(originMeshBuilder.seed);
        int originalSeed = originMeshBuilder.seed;
        int res = originMeshBuilder.resolution;
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
                HexSide uS = C.UpstreamSideOf(truePos, mapSize, construct);
                HexSide dS = C.DownstreamSideOf(truePos, mapSize, construct);
                originMeshBuilder.GenerateWithRiverNodes(uS, dS);
                GameObject newLandHex = Instantiate(originLandHex, transform);
                GameObject newWaterHex = Instantiate(originWaterHex, transform);
                GameObject newDam = Instantiate(originDam.gameObject, transform);
                Vector2 pos = new(
                    rowOrg.x + (j * hexOff.x),
                    rowOrg.y + (j * hexOff.y) + (hexW * 2f)
                );
                newLandHex.transform.position = new(pos.x, 0f, pos.y);
                newWaterHex.transform.position = new(pos.x, waterHeight, pos.y);
                Vector2 damPos = Geometry.EquivHexPos(dS, hexW);
                newDam.transform.position = new(
                    pos.x + damPos.x, waterHeight-originMeshBuilder.hillHeight, pos.y + damPos.y);
                newLandHex.name = "Land-" + truePos.x + "-" + truePos.y;
                newWaterHex.name = "Water-" + truePos.x + "-" + truePos.y;
                newDam.name = "Dam-" + truePos.x + "-" + truePos.y;
                hexes.Add(truePos, new(truePos, newLandHex, newWaterHex, 
                    newDam.GetComponent<BeaverDam>()));
            }
            rowOrg.x += (i < mapSize / 2) ? -hexOff.x : 0;
            rowOrg.y += (i < mapSize / 2) ? hexOff.y : 2f * hexOff.y;
        }
        // reset the seed
        originMeshBuilder.seed = originalSeed;
        // build game world
        List<List<Hex>> wRivers = new();
        List<Tuple<Hex, Hex>> wRoads = new();
        foreach (Vector2Int[] river in C.RiverSets(construct))
        {
            List<Hex> hs = new();
            foreach (Vector2Int v in river)
            {
                hs.Add(hexes[v]);
            }
            wRivers.Add(hs);
        }
        foreach (Vector2Int[] road in C.RoadSets(construct))
        {
            Tuple<Hex, Hex> hs = new(hexes[road[0]], hexes[road[1]]);
            wRoads.Add(hs);
        }
        //GameWorld.Instance().AddWorld(new(wRivers, wRoads));
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
