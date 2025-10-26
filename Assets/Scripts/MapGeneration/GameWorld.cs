using UnityEngine;
using WorldUtil;

public class GameWorld : MonoBehaviour
{
    private static GameWorld _instance;

    private World _storedWorld;

    private void Start()
    {
        _instance = this;
    }

    public static GameWorld Instance() => _instance;

    public void AddWorld(World world) => _storedWorld = world;

    public World World() => _storedWorld;
}
