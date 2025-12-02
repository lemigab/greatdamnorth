using UnityEngine;
using System.Collections;
using WorldUtil;
using Unity.Netcode;

public class BeaverDam : NetworkBehaviour
{
    private NetworkVariable<int> _lvlNetworked = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public const int MAX_LVL = 2; // arbitrary for now
    public const float LVL_MULT = 2f;

    public int Level() => _lvlNetworked.Value;

    private void SetScale() =>
        gameObject.transform.localScale = new(6f, 0.2f + LVL_MULT * _lvlNetworked.Value, 2f);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _lvlNetworked.OnValueChanged += OnDamLevelChanged;
        SetScale();
        
        if (IsServer)
        {
            StartCoroutine(DelayedWaterUpdate());
        }
    }
    
    private System.Collections.IEnumerator DelayedWaterUpdate()
    {
        int maxWaitFrames = 60;
        int waited = 0;
        
        while (waited < maxWaitFrames)
        {
            if (GameWorld.Instance() != null && GameWorld.Instance().World() != null)
            {
                Hex at = GameWorld.Instance().World().FindHexWithDam(this);
                if (at != null)
                {
                    SetWaterHexes();
                    yield break;
                }
            }
            yield return null;
            waited++;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        _lvlNetworked.OnValueChanged -= OnDamLevelChanged;
        base.OnNetworkDespawn();
    }
    
    private void OnDamLevelChanged(int previousValue, int newValue)
    {
        SetScale();
        
        if (IsServer)
        {
            SetWaterHexes();
        }
    }
    
    private void SetWaterHexes()
    {
        float dL = GameWorld.Instance().DefaultWaterHeight();
        World w = GameWorld.Instance().World();
        Hex at = w.FindHexWithDam(this);
        
        // see if a downstream dam is larger
        foreach (Hex h in w.DownstreamFrom(at, false))
            if (h.exitDam.Level() > _lvlNetworked.Value) return;
        
        foreach (Hex h in w.UpstreamFrom(at, true))
        {
            if (h.exitDam.Level() > _lvlNetworked.Value) break;
            h.SetWaterLevel(_lvlNetworked.Value);
            
            if (h.waterMesh != null)
            {
                Vector3 basePos = h.waterMesh.transform.position;
                Vector3 newPos = new(basePos.x, -dL + _lvlNetworked.Value * OffsetPerLevel(), basePos.z);
                h.waterMesh.transform.position = newPos;
            }
        }
    }

    [ContextMenu("Increment")]
    public bool Increment()
    {
        if (!IsServer) return false;
        if (_lvlNetworked.Value == MAX_LVL) return false;
        
        _lvlNetworked.Value++;
        return true;
    }

    [ContextMenu("Decrement")]
    public bool Decrement()
    {
        if (!IsServer) return false;
        if (_lvlNetworked.Value == 0) return false;
        
        _lvlNetworked.Value--;
        return true;
    }

    public float OffsetPerLevel() => (LVL_MULT / 2f);
}