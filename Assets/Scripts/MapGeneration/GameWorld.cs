using Unity.VisualScripting;
using UnityEngine;
using WorldUtil;

public class GameWorld : MonoBehaviour
{
    public TradeLeaderboard leaderboard;
    public BeaverController lordsChosenBeaver;

    private static GameWorld _instance;

    private World _storedWorld;

    private float _defaultWaterHeight;

    private void Awake()
    {
        _instance = this;
    }

    public static GameWorld Instance() => _instance;

    public void SetWaterHeight(float h) => _defaultWaterHeight = h;
    public float DefaultWaterHeight() => _defaultWaterHeight;

    public void AddWorld(World world)
    {
        _storedWorld = world;
        leaderboard.ClearAll();
        leaderboard.Init(_storedWorld);
    }

    public World World() => _storedWorld;


    // Use this if not in game mode
    [ContextMenu("Initialize")]
    public void Init() => _instance = this;
}
