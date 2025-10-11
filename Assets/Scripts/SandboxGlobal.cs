using System.Collections;
using UnityEngine;

public class SandboxGlobal : MonoBehaviour
{
    // Scene object references
    public GameObject playerMouthLog;
    public GameObject enemyMouthLog;
    public GameObject playerDam;
    public GameObject enemyDam;

    // Acess this script globally
    private static SandboxGlobal _instance;
    public SandboxGlobal GetInstance() => _instance;

    // Setup code
    private void Start()
    {
        _instance = this;
        playerMouthLog.SetActive(false);
        enemyMouthLog.SetActive(false);
        playerDam.SetActive(false);
        enemyDam.SetActive(false);
    }


    // States of log in mouth
    // Changing the state sets the visibility of the actual object
    private bool _playerHoldingLog = false;
    private bool _enemyHoldingLog = false;

    public bool PlayerHoldingLog
    {
        get { return _playerHoldingLog; }
        set { _playerHoldingLog = value; playerMouthLog.SetActive(value); }
    }

    public bool EnemyHoldingLog
    {
        get { return _enemyHoldingLog; }
        set { _enemyHoldingLog = value; enemyMouthLog.SetActive(value); }
    }


    // States of dam level
    // State of <1 will turn log invisible
    private short _playerDamLevel = 0;
    private short _enemyDamLevel = 0;

    public short PlayerDamLevel
    {
        get { return _playerDamLevel; }
        set { _playerDamLevel = value; playerDam.SetActive(value > 0); }
    }

    public short EnemyDamLevel
    {
        get { return _enemyDamLevel; }
        set { _enemyDamLevel = value; enemyDam.SetActive(value > 0); }
    }


    // Trees may be accessed independent from this script
    // Probably just hide them from view when eaten? Or maybe not...

}
