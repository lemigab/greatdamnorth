using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WorldUtil;
using Random = System.Random;

public class WorldBuilder : MonoBehaviour
{
    public MeshBuilder originMeshBuilder;
    public GameObject originLandHex;
    public GameObject originWaterHex;

    public int mapSize; // longest diameter
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
        float landY = originLandHex.transform.position.y;
        float waterY = originWaterHex.transform.position.y;
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
                Debug.Log(truePos.ToString());
                originMeshBuilder.GenerateWithRiverNodes(
                    Constructs.UpstreamSideOf(truePos, mapSize, construct),
                    Constructs.DownstreamSideOf(truePos, mapSize, construct)
                );
                GameObject newLandHex = Instantiate(originLandHex, transform);
                GameObject newWaterHex = Instantiate(originWaterHex, transform);
                Vector2 pos = new Vector3(
                    rowOrg.x + (j * hexOff.x),
                    rowOrg.y + (j * hexOff.y) + (hexW * 2f)
                );
                newLandHex.transform.position = new(pos.x, landY, pos.y);
                newWaterHex.transform.position = new(pos.x, waterY, pos.y);
                newLandHex.name = "Land-" + truePos.x + "-" + truePos.y;
                newWaterHex.name = "Water-" + truePos.x + "-" + truePos.y;
                hexes.Add(truePos, new(truePos, newLandHex, newWaterHex));
            }
            rowOrg.x += (i < mapSize / 2) ? -hexOff.x : 0;
            rowOrg.y += (i < mapSize / 2) ? hexOff.y : 2f * hexOff.y;
        }
        // reset the seed
        originMeshBuilder.seed = originalSeed;
        // build game world
        List<List<Hex>> wRivers = new();
        List<Tuple<Hex, Hex>> wRoads = new();
        foreach (Vector2Int[] river in Constructs.RiverSets(construct))
        {
            List<Hex> hs = new();
            foreach (Vector2Int v in river)
            {
                hs.Add(hexes[v]);
            }
            wRivers.Add(hs);
        }
        foreach (Vector2Int[] road in Constructs.RoadSets(construct))
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
