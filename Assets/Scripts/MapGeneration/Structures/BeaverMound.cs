using UnityEngine;
using WorldUtil;

public class BeaverMound : MonoBehaviour
{
    private bool _built = false;

    private SyrupFarm _ctrl = null;

    public bool IsBuilt() => _built;

    public SyrupFarm Controller() => _ctrl;

    public void Build(SyrupFarm builder)
    {
        _ctrl = builder;
        _built = true;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }

    // Using the menu makes you play as Beaver0
    [ContextMenu("Build")]
    public void Build() => Build(GameWorld.Instance().World().syrupFarms[0]);

    [ContextMenu("Dismantle")]
    public void Dismantle()
    {
        _ctrl = null;
        _built = false;
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}