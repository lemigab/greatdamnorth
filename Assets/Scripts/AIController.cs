using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the AI beaver using a finite state machine.
/// Reuses BeaverController for movement and actions.
/// </summary>
[RequireComponent(typeof(BeaverController))]
public class AIController : MonoBehaviour
{
    private enum AIState
    {
        Patrol_NoLog,
        Patrol_Log,
        ChewTree,
        BuildDam,
        BreakDam
    }

    private AIState currentState = AIState.Patrol_NoLog;
    private BeaverController beaver;
    private SandboxGlobal global;

    // Movement
    private Vector3 patrolTarget;
    private float patrolRadius = 50f;
    private float patrolSpeed = 2f;
    private float detectionRange = 5f;

    // Timer control
    private float actionTimer = 0f;
    private float chewDuration = 3f;
    private float buildDuration = 3f;
    private float breakDuration = 3f;

    void Start()
    {
        beaver = GetComponent<BeaverController>();
        global = Object.FindFirstObjectByType<SandboxGlobal>();
        PickNewPatrolPoint();
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrol_NoLog:
                Patrol_NoLog();
                break;
            case AIState.Patrol_Log:
                Patrol_Log();
                break;
            case AIState.ChewTree:
                ChewTree();
                break;
            case AIState.BuildDam:
                BuildDam();
                break;
            case AIState.BreakDam:
                BreakDam();
                break;
        }
    }

    // ----------- STATE LOGIC -----------

    private void Patrol_NoLog()
    {
        MoveTowards(patrolTarget);

        // If reached target, pick a new one
        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            PickNewPatrolPoint();

        // Check if near a tree
        if (beaver.isNearTree)
        {
            currentState = AIState.ChewTree;
            actionTimer = chewDuration;
            return;
        }

        // Decide if we should break or build dams
        if (global != null)
        {
            if (global.EnemyDamLevel > 0)
            {
                currentState = AIState.BreakDam;
                actionTimer = breakDuration;
                return;
            }
            else
            {
                currentState = AIState.BuildDam;
                actionTimer = buildDuration;
                return;
            }
        }
    }

    private void Patrol_Log()
    {
        MoveTowards(patrolTarget);

        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            PickNewPatrolPoint();

        // If close to damming area (placeholder condition)
        if (Random.value < 0.01f) // TODO: Replace with distance check to real build zone
        {
            currentState = AIState.BuildDam;
            actionTimer = buildDuration;
        }
    }

    private void ChewTree()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            beaver.Chew();
            global.EnemyHoldingLog = true;  // AI beaver now "has a log"
            currentState = AIState.Patrol_Log;
        }
    }

    private void BuildDam()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            beaver.BuildDam();
            global.EnemyDamLevel += 1;
            global.EnemyHoldingLog = false;
            currentState = AIState.Patrol_NoLog;
        }
    }

    private void BreakDam()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            beaver.BreakDam();
            global.EnemyDamLevel = 0; // Reset enemy dam level
            currentState = AIState.Patrol_NoLog;
        }
    }

    // ----------- HELPER METHODS -----------

    private void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        beaver.Move(direction);
    }

    private void PickNewPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        patrolTarget = new Vector3(
            transform.position.x + randomCircle.x,
            transform.position.y,
            transform.position.z + randomCircle.y
        );
    }

    
}