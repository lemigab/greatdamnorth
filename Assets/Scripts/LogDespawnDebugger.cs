using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Debug script to track when logs are despawned on clients
/// Attach this to log prefabs to see if despawn messages are being received
/// </summary>
public class LogDespawnDebugger : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[LogDespawnDebugger] Log {gameObject.name} spawned on client. NetworkObjectId: {NetworkObjectId}, IsServer: {IsServer}, IsClient: {IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log($"[LogDespawnDebugger] Log {gameObject.name} DESPAWNED on client! NetworkObjectId: {NetworkObjectId}, IsServer: {IsServer}, IsClient: {IsClient}, Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
        
        // Despawn(true) should destroy the GameObject, but if it's still visible, hide it
        // This is a workaround - Despawn should handle this automatically
        if (IsClient && !IsServer)
        {
            // Hide the log on client since Despawn(true) should destroy it but might not be working
            gameObject.SetActive(false);
            
            // Also disable renderer as backup
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            Debug.Log($"[LogDespawnDebugger] Set log {gameObject.name} inactive and disabled renderer on client after despawn");
        }
    }

    void OnDestroy()
    {
        Debug.Log($"[LogDespawnDebugger] Log {gameObject.name} destroyed. IsSpawned: {(NetworkObject != null ? NetworkObject.IsSpawned.ToString() : "N/A")}");
    }
}

