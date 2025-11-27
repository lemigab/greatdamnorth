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
            Vector3 v = h.waterMesh.transform.position;
            h.waterMesh.transform.position =
                new(v.x, -dL + _lvl * OffsetPerLevel(), v.z);
        }
    }

    public float OffsetPerLevel() => (LVL_MULT / 2f);
}