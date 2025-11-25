using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using WorldUtil;

public class BoatSpawner : MonoBehaviour
{
    // Make sure the BoatSpawner object is at exactly {0,0,0}
    // Otherwise they won't know where the rivers are

    public PreviewBoat originBoat;
    public int spawnInterval = 64;
    public bool spawnerOn = false;

    private int frameCount = 0;

    private void FixedUpdate()
    {
        if (!spawnerOn) return;
        if (frameCount++ % spawnInterval != 0) return;

        // Summon a PreviewBoat at each syrup farm location
        // The boat will autonomously travel downstream
        //   so from here we can just let it do its thing
        int boatCount = 1;
        foreach (SyrupFarm f in GameWorld.Instance().World().syrupFarms)
        {
            Vector3 spawnPoint = f.location.waterMesh
                .GetComponent<MeshRenderer>().bounds.center;
            GameObject boat = Instantiate(originBoat.gameObject, transform);
            boat.transform.position = spawnPoint;
            boat.name = "Boat" + boatCount++;
            boat.GetComponent<PreviewBoat>().SetTarget(spawnPoint);
        }
    }
}
