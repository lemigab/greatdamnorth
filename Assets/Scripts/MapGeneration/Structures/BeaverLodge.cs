using UnityEngine;
using WorldUtil;
using Unity.Netcode;

public class BeaverLodge : NetworkBehaviour
{
    private NetworkVariable<bool> _built = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private SyrupFarm _ctrl = null;

    public bool IsBuilt() => _built.Value;

    public SyrupFarm Controller() => _ctrl;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _built.OnValueChanged += OnBuiltChanged;
        UpdateVisibility();
    }

    public override void OnNetworkDespawn()
    {
        _built.OnValueChanged -= OnBuiltChanged;
        base.OnNetworkDespawn();
    }

    private void OnBuiltChanged(bool previousValue, bool newValue)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = _built.Value;
        }
    }

    public void Build(SyrupFarm builder)
    {
        if (NetworkManager.Singleton != null && !IsServer) return;
        
        _ctrl = builder;
        _built.Value = true;
        if (NetworkManager.Singleton == null)
        {
            UpdateVisibility();
        }
        GameWorld.Instance().leaderboard.RefreshTradeControl();
    }

    [ContextMenu("Build")]
    public void AdminBuild()
        => Build(GameWorld.Instance().lordsChosenBeaver.GetHomeFarm());

    [ContextMenu("Dismantle")]
    public void Dismantle()
    {
        if (NetworkManager.Singleton != null && !IsServer) return;
        
        _ctrl = null;
        _built.Value = false;
        if (NetworkManager.Singleton == null)
        {
            UpdateVisibility();
        }
        GameWorld.Instance().leaderboard.RefreshTradeControl();
    }
}