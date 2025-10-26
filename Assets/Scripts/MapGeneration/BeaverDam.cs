using UnityEngine;
using WorldUtil;

public class BeaverDam : MonoBehaviour
{

    private int _lvl = 1; // later make this 0

    public const int MAX_LVL = 4; // arbitrary for now

    public int Level() => _lvl;


    // Try to build one more level
    // Return true if success
    [ContextMenu("Increment")]
    public bool Increment()
    {
        if (_lvl == MAX_LVL) return false;
        _lvl++;
        gameObject.transform.localScale = new(_lvl, _lvl, _lvl);
        return true;
    }

    // Try to dismantle one level
    // Return true if success
    [ContextMenu("Decrement")]
    public bool Decrement()
    {
        if (_lvl == 0) return false;
        _lvl--;
        gameObject.transform.localScale = new(_lvl, _lvl, _lvl);
        return true;
    }

}
