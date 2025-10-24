using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class WorldBuilder : MonoBehaviour
{
    public MeshBuilder originMeshBuilder;
    public GameObject originLandHex;
    public GameObject originWaterHex;

    public int mapSize; // longest diameter
    public bool isTri;

    private List<GameObject> _created = new();


    [ContextMenu("Construct Map")]
    public void ConstructMap()
    {
        Clear();
        Random rng = new(originMeshBuilder.seed);
        int originalSeed = originMeshBuilder.seed;
        float landY = originLandHex.transform.position.y;
        float waterY = originWaterHex.transform.position.y;
        int res = originMeshBuilder.resolution;
        float hexW = (res - (res % 2 != 0 ? 1f : 0f)) * originMeshBuilder.scale;
        float sq3 = (float)Math.Sqrt(3f);
        Vector2 hexOff = new(hexW * 0.75f, hexW * 0.25f * sq3);
        Vector2 rowOrg = new(0f, 0f);
        if (!isTri && mapSize % 2 == 0) mapSize--;
        for (int i = 0; i < mapSize; i++)
        {
            int rowLen = isTri
                ? mapSize - i
                : mapSize - Math.Abs(i - (mapSize / 2));
            for (int j = 0; j < rowLen; j++)
            {
                if (i == 0 && j == 0) continue;
                originMeshBuilder.seed = rng.Next(999999);
                originMeshBuilder.Generate();
                GameObject newLandHex = Instantiate(originLandHex);
                GameObject newWaterHex = Instantiate(originWaterHex);
                Vector2 pos = new Vector3(
                    rowOrg.x + (j * hexOff.x),
                    rowOrg.y + (j * hexOff.y)
                );
                newLandHex.transform.position = new(pos.x, landY, pos.y);
                newWaterHex.transform.position = new(pos.x, waterY, pos.y);
                _created.Add(newLandHex);
                _created.Add(newWaterHex);
            }
            rowOrg.x += !isTri && (i < mapSize / 2) ? -hexOff.x : 0f;
            rowOrg.y += !isTri && (i < mapSize / 2) ? hexOff.y : 2f * hexOff.y;
        }
        originMeshBuilder.seed = originalSeed;
    }


    [ContextMenu("Clear")]
    public void Clear()
    {
        foreach (GameObject o in _created) DestroyImmediate(o);
        _created.Clear();
    }
}
