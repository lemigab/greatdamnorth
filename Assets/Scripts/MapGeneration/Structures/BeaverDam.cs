using UnityEngine;
using WorldUtil;

public class BeaverDam : MonoBehaviour
{
    private int _lvl = 0;

    public const int MAX_LVL = 2; // arbitrary for now

    public const float LVL_MULT = 2f;

    public int Level() => _lvl;

    private void SetScale() =>
        gameObject.transform.localScale = new(6f, 0.2f + LVL_MULT * _lvl, 2f);


    // Try to build one more level
    // Return true if success
    [ContextMenu("Increment")]
    public bool Increment()
    {
        if (_lvl == MAX_LVL) return false;
        _lvl++; SetScale(); SetWaterHexes();
        return true;
    }

    // Try to dismantle one level
    // Return true if success
    [ContextMenu("Decrement")]
    public bool Decrement()
    {
        if (_lvl == 0) return false;
        _lvl--; SetScale(); SetWaterHexes();
        return true;
    }


    // Try to flood/dry upstream water hexes to this dam's new level
    // If a larger dam exists UPSTREAM, stop at it
    // If a larger dam exists DOWNSTREAM, do not move any water
    private void SetWaterHexes()
    {
        float dL = GameWorld.Instance().DefaultWaterHeight();
        World w = GameWorld.Instance().World();
        Hex at = w.FindHexWithDam(this);
        // see if a downstream dam is larger
        foreach (Hex h in w.DownstreamFrom(at, false))
            if (h.exitDam.Level() > _lvl) return;
        // set home hex
        // set upstream hexes
        foreach (Hex h in w.UpstreamFrom(at, true))
        {
            if (h.exitDam.Level() > _lvl) break;
            h.SetWaterLevel(_lvl);
            
            // Calculate the new position
            Vector3 basePos = h.waterMesh.transform.position;
            Vector3 newPos = new(basePos.x, -dL + _lvl * OffsetPerLevel(), basePos.z);
            
            // ALWAYS update the local water hex first (this is what's stored in Hex class)
            // On clients, this might be what's actually visible
            h.waterMesh.transform.position = newPos;
            
            // Also find and update the NetworkObject version if it exists
            // On server, the local one IS the NetworkObject. On clients, they're separate.
            string waterName = h.waterMesh.name;
            GameObject networkWaterHex = null;
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                // Search through all spawned NetworkObjects
                foreach (var spawnedObj in Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
                {
                    if (spawnedObj.gameObject.name == waterName)
                    {
                        // Verify it's a water hex by checking for MeshRenderer
                        MeshRenderer checkMR = spawnedObj.gameObject.GetComponent<MeshRenderer>();
                        if (checkMR != null)
                        {
                            networkWaterHex = spawnedObj.gameObject;
                            break;
                        }
                    }
                }
            }
            
            // Update the local water hex (stored in Hex class) - this is what's visible on both server and clients
            h.waterMesh.transform.position = newPos;
            
            // Also update NetworkObject version if it exists and is different
            if (networkWaterHex != null && networkWaterHex != h.waterMesh)
            {
                networkWaterHex.transform.position = newPos;
            }
            
            // Force visual refresh on the local water hex (primary one)
            MeshRenderer mr = h.waterMesh.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // Force renderer refresh
                bool wasEnabled = mr.enabled;
                mr.enabled = false;
                mr.enabled = wasEnabled;
            }
            
            // Force mesh bounds update
            MeshFilter mf = h.waterMesh.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh mesh = mf.sharedMesh;
                mesh.RecalculateBounds();
                mf.sharedMesh = null;
                mf.sharedMesh = mesh;
            }
        }
    }

    public float OffsetPerLevel() => (LVL_MULT / 2f);
}