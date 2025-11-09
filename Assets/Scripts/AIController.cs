using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the AI beaver using a finite state machine.
/// Reuses BeaverController for movement and actions.
/// </summary>
[RequireComponent(typeof(BeaverController))]
public class AIController : BeaverController
{
    public enum AIState
    {
        Patrol_With_No_Log,
        Patrol_With_Log,
        Chew_Tree,
        Build_Dam,
        Break_Dam
    }

    private AIState currentState = AIState.Patrol_With_No_Log;

    public AIState GetState() => currentState;

    private SandboxGlobal global;

    private GameObject targetTree;
    private GameObject targetDam;

    // Movement
    private Vector3 patrolTarget;
    private float patrolRadius = 50f;
    private float patrolSpeed = 2f;
    private float detectionRange = 5f;

    // Timer control
    private float actionTimer = 0f;
    private float chewDuration = 1.5f;
    private float buildDuration = 1.5f;
    private float breakDuration = 1.5f;

    private Collider currentTreeCollider;
    private Collider currentDamCollider;

    void Start()
    {
        global = SandboxGlobal.GetInstance();
        PickNewPatrolPoint();
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrol_With_No_Log:
                Patrol_NoLog();
                break;
            case AIState.Patrol_With_Log:
                Patrol_Log();
                break;
            case AIState.Chew_Tree:
                ChewTree();
                break;
            case AIState.Build_Dam:
                BuildDam();
                break;
            case AIState.Break_Dam:
                BreakDam();
                break;
        }
    }

    // ----------- STATE LOGIC -----------

    private void Patrol_NoLog()
    {
        // 1) Try to break enemy dam first
        if (global != null && global.enemyDam != null && global.EnemyDamLevel > 0)
        {
            targetDam = global.enemyDam;
            MoveTowards(targetDam.transform.position);

            // Only switch to break state if actually touching dam collider
            if (currentDamCollider != null && currentDamCollider.gameObject == targetDam)
            {
                currentState = AIState.Break_Dam;
                actionTimer = breakDuration;
            }
            return;
        }

        // 2) Otherwise, find nearest tree
        if (targetTree == null || !targetTree.activeInHierarchy)
            targetTree = FindNearestTree();

        if (targetTree != null)
        {
            MoveTowards(targetTree.transform.position);

            // Only chew if physically touching the tree
            if (currentTreeCollider != null && currentTreeCollider.gameObject == targetTree)
            {
                currentState = AIState.Chew_Tree;
                actionTimer = chewDuration;
            }
            return;
        }

        // 3) Fallback wandering
        MoveTowards(patrolTarget);
        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            PickNewPatrolPoint();
    }

    private void Patrol_Log()
    {
        if (global != null && global.playerDam != null)
        {
            targetDam = global.playerDam;

            // Only move toward the dam's XZ, keep current Y
            Vector3 damPos = targetDam.transform.position;
            Vector3 moveTarget = new Vector3(damPos.x, transform.position.y, damPos.z);

            MoveTowards(moveTarget);

            // When close enough on X/Z, start building
            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatDam = new Vector3(damPos.x, 0, damPos.z);
            float distance = Vector3.Distance(flatPos, flatDam);

            if (distance < 0.5f) // tweak stop distance as needed
            {
                currentState = AIState.Build_Dam;
                actionTimer = buildDuration;
            }

            return;
        }

        // Fallback wandering
        MoveTowards(patrolTarget);
        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            PickNewPatrolPoint();
    }
    private void ChewTree()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            base.Chew();
            //global.EnemyHoldingLog = true;  // AI beaver now "has a log"
            currentState = AIState.Patrol_With_Log;
        }
    }

    private void BuildDam()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            base.BuildDam();
            //global.PlayerDamLevel += 1;
            //global.EnemyHoldingLog = false;
            targetTree = FindNearestTree();
            currentState = AIState.Patrol_With_No_Log;
        }
    }

    private void BreakDam()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            base.BreakDam();
            //global.EnemyHoldingLog = true;  // AI beaver now "has a log"
            currentState = AIState.Patrol_With_Log;
        }
    }

    // ----------- HELPER METHODS -----------

    private void MoveTowards(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;  // Flatten the movement
        direction.Normalize();
        base.Move(direction);
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

    private GameObject FindNearestTree()
    {
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject tree in trees)
        {
            float dist = Vector3.Distance(transform.position, tree.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = tree;
            }
        }
        return nearest;
    }  

    private void OnTriggerEnter(Collider other)
{
    // Detect trees and dams by tag or name
    if (other.CompareTag("Tree") || other.name.StartsWith("Tree_"))
    {
        currentTreeCollider = other;
    }
    else if (other.gameObject == global.playerDam || other.gameObject == global.enemyDam)
    {
        currentDamCollider = other;
    }
}

private void OnTriggerExit(Collider other)
{
    if (other == currentTreeCollider)
        currentTreeCollider = null;

    if (other == currentDamCollider)
        currentDamCollider = null;
}
}