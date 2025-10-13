using System.Collections;
using TMPro;
using UnityEngine;

public class SandboxGlobal : MonoBehaviour
{
    // Scene object references
    public GameObject playerMouthLog;
    public GameObject enemyMouthLog;
    public GameObject playerDam;
    public GameObject enemyDam;

    // Background references
    public TextMeshProUGUI playerLevel;
    public TextMeshProUGUI enemyLevel;
    public TextMeshProUGUI enemyState;
    public AIController enemyAI;

    // Acess this script globally
    private static SandboxGlobal _instance;
    public static SandboxGlobal GetInstance() => _instance;

    private static Vector3 s;

    // Setup code
    private void Start()
    {
        _instance = this;
        s = playerDam.transform.localScale;
        playerMouthLog.GetComponent<MeshRenderer>().enabled = false;
        enemyMouthLog.GetComponent<MeshRenderer>().enabled = false;
        playerDam.GetComponent<MeshRenderer>().enabled = false;
        enemyDam.GetComponent<MeshRenderer>().enabled = false;
    }


    // Background updates
    private void FixedUpdate()
    {
        enemyState.text = "Enemy FSM State:\n" + enemyAI.GetState().ToString();
    }


    // States of log in mouth
    // Changing the state sets the visibility of the actual object
    private bool _playerHoldingLog = false;
    private bool _enemyHoldingLog = false;

    public bool PlayerHoldingLog
    {
        get { return _playerHoldingLog; }
        set { _playerHoldingLog = value; playerMouthLog.GetComponent<MeshRenderer>().enabled = value; }
    }

    public bool EnemyHoldingLog
    {
        get { return _enemyHoldingLog; }
        set { _enemyHoldingLog = value; enemyMouthLog.GetComponent<MeshRenderer>().enabled = value; }
    }


    // States of dam level
    // State of <1 will turn log invisible
    private short _playerDamLevel = 0;
    private short _enemyDamLevel = 0;

    public short PlayerDamLevel
    {
        get { return _playerDamLevel; }
        set
        {
            _playerDamLevel = value;
            playerLevel.text = "Dam Lvl - " + value;
            playerDam.GetComponent<MeshRenderer>().enabled = (value > 0);
            float dlt = (value - 1) * 8f;
            if (value > 0) playerDam.transform.localScale
                    = new(s.x + dlt, s.y + dlt, s.z + dlt);
        }
    }

    public short EnemyDamLevel
    {
        get { return _enemyDamLevel; }
        set
        {
            _enemyDamLevel = value;
            enemyLevel.text = "Dam Lvl - " + value;
            enemyDam.GetComponent<MeshRenderer>().enabled = (value > 0);
            float dlt = (value - 1) * 8f;
            if (value > 0) enemyDam.transform.localScale
                    = new(s.x + dlt, s.y + dlt, s.z + dlt);
        }
    }


    // Trees may be accessed independent from this script
    // Probably just hide them from view when eaten? Or maybe not...

}
