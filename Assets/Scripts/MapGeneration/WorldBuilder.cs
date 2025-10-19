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

    public int mapSize; // map will be a 'square' of this length


    private List<GameObject> _created = new();


    [ContextMenu("Construct Map")]
    public void ConstructMap()
    {
        Clear();
        Random rng = new(originMeshBuilder.seed);
        int originalSeed = originMeshBuilder.seed;
        float landY = originLandHex.transform.position.y;
        float waterY = originWaterHex.transform.position.y;
        for (int i = 0; i < mapSize; i++)
        {
            Vector2 rowOrg = new(
                i * 186.5f,
                i % 2 == 0 ? 0.0f : 107.5f
            );
            for (int j = 0; j < mapSize; j++)
            {
                if (i == 0 && j == 0) continue;
                originMeshBuilder.seed = rng.Next(999999);
                originMeshBuilder.Generate();
                GameObject newLandHex = Instantiate(originLandHex);
                GameObject newWaterHex = Instantiate(originWaterHex);
                Vector2 pos = new Vector3(
                    rowOrg.x,
                    rowOrg.y + (j * 214.5f)
                );
                newLandHex.transform.position = new(pos.x, landY, pos.y);
                newWaterHex.transform.position = new(pos.x, waterY, pos.y);
                _created.Add(newLandHex);
                _created.Add(newWaterHex);
            }
        }
        originMeshBuilder.seed = originalSeed;
    }


    [ContextMenu("Clear")]
    public void Clear()
    {
        foreach (GameObject o in  _created) DestroyImmediate(o);
        _created.Clear();
    }
}
