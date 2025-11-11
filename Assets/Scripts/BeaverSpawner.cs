using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldUtil;

/// <summary>
/// Handles spawning beavers at designated spawn positions.
/// </summary>
public class BeaverSpawner : MonoBehaviour
{
    public GameObject playerBeaverPrefab;
    public GameObject aiBeaverPrefab;
    
    public List<Vector2Int> aiSpawnTiles = new List<Vector2Int> { new Vector2Int(3, 0), new Vector2Int(6, 3), new Vector2Int(6, 6), new Vector2Int(3, 6), new Vector2Int(0, 3) };
    public Vector2Int playerSpawnTiles = new Vector2Int(0, 0);
    
    private bool hasSpawned = false;
    private GameObject playerBeaver;
    private List<GameObject> aiBeavers = new List<GameObject>();
    
    [ContextMenu("Spawn Beavers")]
    public void SpawnBeavers()
    {
        if (hasSpawned)
        {
            Debug.LogWarning("BeaverSpawner: Beavers already spawned!");
            return;
        }
        // Get World instance
        World world = GameWorld.Instance()?.World();
        if (world == null)
        {
            Debug.LogError("BeaverSpawner: World not available! Make sure WorldBuilder has constructed the map.");
            return;
        }
        
        // Spawn player beaver
        /*
        if (playerSpawnTiles != Vector2Int.zero)
        {
            Hex playerHex = world.all.Find(hex => hex.mapPosition == playerSpawnTiles);
            if (playerHex != null)
            {
                Vector3 spawnPos = playerHex.landMesh.transform.position;
                SpawnPlayerBeaver(spawnPos);
            }
            else
            {
                Debug.LogWarning($"BeaverSpawner: Player spawn tile {playerSpawnTiles} not found in world!");
            }
        }
        else
        {
            Debug.LogWarning("BeaverSpawner: Player spawn position not set!");
        }
        */
        
        // Spawn AI beavers
        if (aiSpawnTiles.Count > 0)
        {
            for (int i = 0; i < aiSpawnTiles.Count; i++)
            {
                Hex aiHex = world.all.Find(hex => hex.mapPosition == aiSpawnTiles[i]);
                if (aiHex != null)
                {
                    Vector3 spawnPos = aiHex.landMesh.transform.position;
                    SpawnAIBeaver(spawnPos, i);
                }
                else
                {
                    Debug.LogWarning($"BeaverSpawner: AI spawn tile {aiSpawnTiles[i]} not found in world!");
                }
            }
        }
        else
        {
            Debug.LogWarning("BeaverSpawner: No AI spawn positions available!");
        }
        
        hasSpawned = true;
    }
    
    private void SpawnPlayerBeaver(Vector3 position)
    {
        playerBeaver = Instantiate(playerBeaverPrefab, position, Quaternion.identity);
        playerBeaver.name = "PlayerBeaver";
        
        Debug.Log($"Spawned player beaver at {position}");
    }
    
    private void SpawnAIBeaver(Vector3 position, int index)
    {
        GameObject aiBeaver;
        aiBeaver = Instantiate(aiBeaverPrefab, position, Quaternion.identity);
        aiBeaver.name = "AIBeaver-" + index;
        
        aiBeavers.Add(aiBeaver);
        
        Debug.Log($"Spawned AI beaver {index} at {position}");
    }
    
}

