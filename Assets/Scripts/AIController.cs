using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System.Collections.Generic;
using WorldUtil;

/// <summary>
/// Controls the AI beaver using a finite state machine.
/// Reuses BeaverController for actions (chew/build/break),
/// and AINavMesh/NavMeshAgent for movement.
/// </summary>
[RequireComponent(typeof(BeaverController))]
[RequireComponent(typeof(AINavMesh))]
[RequireComponent(typeof(NavMeshAgent))]
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

    private World world;

    // Hexes that make up the river this beaver spawned on
    private List<Hex> myRiverHexes = new List<Hex>();

    // Trees that sit on those hexes
    private List<GameObject> myRiverTrees = new List<GameObject>();

    private bool worldInitialized = false;

    // Movement
    private Vector3 patrolTarget;
    private float patrolRadius = 50f;
    // private float patrolSpeed = 2f;   // NOTE: NavMeshAgent speed is on the agent now
    // private float detectionRange = 5f;

    // Timer control
    private float actionTimer = 0f;
    private float chewDuration = 1.5f;
    private float buildDuration = 1.5f;
    private float breakDuration = 1.5f;

    // Dams along my river (hexes that have exitDam != null)
    private List<Hex> myRiverDamHexes = new List<Hex>();

    // Which dam index along my river I'm currently targeting for building
    private int currentDamBuildIndex = 0;

    private Collider currentTreeCollider;
    private Collider currentDamCollider;

    // NEW: reference to NavMesh movement helper
    private AINavMesh nav;

    private NavMeshAgent agent;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private float stuckCheckInterval = 2f;
    private float stuckThreshold = 0.2f;

    public override void Start()
    {
        base.Start();
        global = SandboxGlobal.GetInstance();
        nav = GetComponent<AINavMesh>();
        agent = GetComponent<NavMeshAgent>();
        
        if (base.rb != null)
        {
            base.rb.isKinematic = true;
            base.rb.useGravity = false;
        }
        
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.updateUpAxis = false;
            
            agent.speed = 2f;
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;
            agent.autoBraking = true;
            agent.autoRepath = true;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            if (agent != null)
            {
                agent.Warp(hit.position);
            }
        }

        if (nav == null)
        {
            Debug.LogError("AIController: AINavMesh component is missing!");
        }

        lastPosition = transform.position;
        PickNewPatrolPoint();
    }

    void Update()
    {
        // Ensure the World is ready before running AI logic
        if (!worldInitialized)
        {
            TryInitWorld();
            if (!worldInitialized)
            {
                // World still not ready this frame; skip AI logic
                return;
            }
        }
        
        // Sync NavMeshAgent with transform (in case of external position changes)
        if (agent != null && agent.enabled)
        {
            float distance = Vector3.Distance(transform.position, agent.nextPosition);
            if (distance > 0.5f)
            {
                agent.Warp(transform.position);
            }
        }
        
        CheckIfStuck();

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
    
    void FixedUpdate()
    {
        // Update the Rigidbody position to match the NavMeshAgent position
        // Since Rigidbody is kinematic, NavMeshAgent controls position
        if (agent != null && agent.enabled && base.rb != null)
        {
            base.rb.MovePosition(agent.nextPosition);
        }
    }
    
    /// <summary>
    /// Detects if the AI is stuck and tries to recover.    
    /// </summary>
    private void CheckIfStuck()
    {
        if (agent == null || !agent.enabled) return;
        
        if (agent.pathPending || agent.isStopped) return;
        
        stuckTimer += Time.deltaTime;
        
        if (stuckTimer >= stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < stuckThreshold)
            {
                Debug.LogWarning($"AI Beaver: Stuck detected! Distance moved: {distanceMoved}");
                
                if (nav != null) nav.Stop();
                
                Vector3 randomOffset = Random.insideUnitSphere * 5f;
                randomOffset.y = 0;
                Vector3 recoveryPos = transform.position + randomOffset;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(recoveryPos, out hit, 5f, NavMesh.AllAreas))
                {
                    if (nav != null) nav.MoveTo(hit.position);
                    Debug.Log($"AI Beaver: Attempting recovery move to {hit.position}");
                }
                else
                {
                    PickNewPatrolPoint();
                    if (nav != null) nav.MoveTo(patrolTarget);
                }
            }
            
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
    }

    // ----------- STATE LOGIC -----------

    private void Patrol_NoLog()
    {
        // 1) FIRST: try to go for the closest tree on the river I spawned on
        if (targetTree == null || !targetTree.activeInHierarchy)
        {
            targetTree = FindNearestRiverTree();
        }

        if (targetTree != null)
        {
            MoveTowards(targetTree.transform.position);

            if (currentTreeCollider != null && currentTreeCollider.gameObject == targetTree)
            {
                currentState = AIState.Chew_Tree;
                actionTimer = chewDuration;
            }
            return;
        }

        // 2) If no river trees left, fall back to any tree in the map
        if (targetTree == null || !targetTree.activeInHierarchy)
        {
            targetTree = FindNearestTree();
        }

        if (targetTree != null)
        {
            MoveTowards(targetTree.transform.position);

            if (currentTreeCollider != null && currentTreeCollider.gameObject == targetTree)
            {
                currentState = AIState.Chew_Tree;
                actionTimer = chewDuration;
            }
            return;
        }

        // 3) Fallback behaviour (your existing dam / wander logic can go here)
        MoveTowards(patrolTarget);
        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
            PickNewPatrolPoint();
    }

    private void Patrol_Log()
    {
        // No world/dams? Just wander as a fallback.
        if (world == null || myRiverDamHexes == null || myRiverDamHexes.Count == 0)
        {
            MoveTowards(patrolTarget);
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
                PickNewPatrolPoint();
            return;
        }

        // Find the first dam along the river that isn't full (Level < MAX_LVL)
        int usableIndex = -1;
        for (int i = 0; i < myRiverDamHexes.Count; i++)
        {
            Hex hex = myRiverDamHexes[i];
            if (hex == null || hex.exitDam == null) continue;

            BeaverDam dam = hex.exitDam;
            if (dam.Level() < BeaverDam.MAX_LVL)
            {
                usableIndex = i;
                break;
            }
        }

        if (usableIndex == -1)
        {
            // All dams on this river are already at MAX_LVL; nothing to build.
            // You can decide to just wander or do something else here.
            MoveTowards(patrolTarget);
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
                PickNewPatrolPoint();
            return;
        }

        currentDamBuildIndex = usableIndex;

        Hex targetHex = myRiverDamHexes[currentDamBuildIndex];
        if (targetHex == null || targetHex.exitDam == null)
        {
            // Defensive: if something went wrong, just wander.
            MoveTowards(patrolTarget);
            if (Vector3.Distance(transform.position, patrolTarget) < 1f)
                PickNewPatrolPoint();
            return;
        }

        // The dam GameObject we want to build on
        targetDam = targetHex.exitDam.gameObject;

        // Move toward the dam's XZ using NavMesh, like Patrol_NoLog
        Vector3 damPos = targetDam.transform.position;
        Vector3 moveTarget = new Vector3(damPos.x, transform.position.y, damPos.z);
        MoveTowards(moveTarget);

        // When close enough on X/Z, start building
        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatDam = new Vector3(damPos.x, 0f, damPos.z);
        float distance = Vector3.Distance(flatPos, flatDam);

        if (currentDam != null && distance < 0.5f) // tweak as needed
        {
            currentState = AIState.Build_Dam;
            actionTimer = buildDuration;

            if (nav != null) nav.Stop();
        }
    }

    private void ChewTree()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            base.Chew();

            // --- PRINT WHERE THE BEAVER IS GOING NEXT ---
            if (myRiverDamHexes != null && myRiverDamHexes.Count > 0)
            {
                // Find the next target dam hex (same logic Patrol_Log uses)
                int targetIndex = currentDamBuildIndex;
                if (targetIndex < 0) targetIndex = 0;
                if (targetIndex >= myRiverDamHexes.Count)
                    targetIndex = myRiverDamHexes.Count - 1;

                Hex targetHex = myRiverDamHexes[targetIndex];
                if (targetHex != null && targetHex.exitDam != null)
                {
                    Debug.Log($"AI Beaver: Chewed a log, now heading to Dam-{targetHex.mapPosition.x}-{targetHex.mapPosition.y}");
                }
                else
                {
                    Debug.Log("AI Beaver: Chewed a log, but no valid dam found on river.");
                }
            }
            else
            {
                Debug.Log("AI Beaver: Chewed a log, but no river dams assigned.");
            }
            // -------------------------------------------------

            currentState = AIState.Patrol_With_Log;
        }
    }

    private new void BuildDam()
    {
        actionTimer -= Time.deltaTime;


        if (actionTimer <= 0f)
        {
            // Get current dam (based on currentDamBuildIndex)
            BeaverDam dam = null;
            if (myRiverDamHexes != null &&
                currentDamBuildIndex >= 0 &&
                currentDamBuildIndex < myRiverDamHexes.Count)
            {
                Hex hex = myRiverDamHexes[currentDamBuildIndex];
                if (hex != null)
                    dam = hex.exitDam;
            }

            // Only try to build if this dam isn't already full
            if (dam != null && dam.Level() < BeaverDam.MAX_LVL)
            {
                // This should call BeaverController's logic which should in turn
                // call dam.Increment() internally (or do equivalent).
                base.BuildDam();
            }

            // After building, if this dam is now full, move to the next dam
            if (dam != null && dam.Level() >= BeaverDam.MAX_LVL)
            {
                currentDamBuildIndex++;
                if (currentDamBuildIndex >= myRiverDamHexes.Count)
                {
                    // Clamp to last index so we don't blow up;
                    // future Patrol_Log() calls will detect that all dams are full.
                    currentDamBuildIndex = myRiverDamHexes.Count - 1;
                }
            }

            // After building, go back to searching for a new log
            targetTree = FindNearestRiverTree() ?? FindNearestTree();
            currentState = AIState.Patrol_With_No_Log;
        }
    }

    private new void BreakDam()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer <= 0f)
        {
            base.BreakDam();
            currentState = AIState.Patrol_With_Log;
        }
    }

    // ----------- HELPER METHODS -----------

    /// <summary>
    /// Now uses NavMesh via AINavMesh instead of base.Move(direction).
    /// Validates path before moving to prevent getting stuck.
    /// </summary>
    private void MoveTowards(Vector3 target)
    {
        if (nav == null || agent == null) return;
        
        if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            float currentTargetDist = Vector3.Distance(agent.destination, target);
            if (currentTargetDist < 1f)
            {
                return;
            }
        }
        
        nav.MoveTo(target);
        
        StartCoroutine(CheckPathAfterDelay(target));
    }
    
    /// <summary>
    /// Checks if path is valid after a short delay, tries alternative if not.
    /// </summary>
    private IEnumerator CheckPathAfterDelay(Vector3 target)
    {
        yield return new WaitForSeconds(0.1f);
        
        if (agent != null && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 10f, NavMesh.AllAreas))
            {
                nav.MoveTo(hit.position);
                Debug.Log($"AI Beaver: Invalid path to {target}, trying nearby position {hit.position}");
            }
            else
            {
                PickNewPatrolPoint();
                nav.MoveTo(patrolTarget);
                Debug.Log($"AI Beaver: No valid path found, picking new patrol point");
            }
        }
    }

    private void PickNewPatrolPoint()
    {
        int attempts = 0;
        Vector3 candidatePos;
        
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            candidatePos = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePos, out hit, 10f, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
                return;
            }
            
            attempts++;
        } while (attempts < 5);
        
        patrolTarget = candidatePos;
        Debug.LogWarning($"AI Beaver: Could not find valid NavMesh position for patrol, using {patrolTarget}");
    }

    /// <summary>
    /// Calculates the NavMesh path distance to a target position.
    /// Returns float.MaxValue if no valid path exists.
    /// </summary>
    private float CalculatePathDistance(Vector3 targetPosition)
    {
        if (agent == null || !agent.enabled) return float.MaxValue;
        
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            return float.MaxValue;
        }
        
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path))
        {
            return float.MaxValue;
        }
        
        if (path.corners.Length < 2)
        {
            return float.MaxValue;
        }
        
        float pathDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        
        return pathDistance;
    }

    private GameObject FindNearestTree()
    {
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        GameObject nearest = null;
        float minPathDist = Mathf.Infinity;

        foreach (GameObject tree in trees)
        {
            if (tree == null || !tree.activeInHierarchy) continue;
            
            float pathDist = CalculatePathDistance(tree.transform.position);
            if (pathDist < minPathDist)
            {
                minPathDist = pathDist;
                nearest = tree;
            }
        }
        return nearest;
    }


    private void OnTriggerEnter(Collider other)
    {
        // Detect trees and dams by tag or name
        if (other.name.StartsWith("Log"))
        {
            isNearLog = true;
            currentLog = other.gameObject;

            currentTreeCollider = other;
        }
        else if (other.name.StartsWith("Dam-"))
        {
            isNearDam = true;
            currentDam = other.gameObject;

            currentDamCollider = other;
        }
        else if (other.name.StartsWith("Water"))
        {
            agent.speed = 8f;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other == currentTreeCollider)
        {
            isNearLog = false;
            if (currentLog == other.gameObject)
            {
                currentLog = null;
            }
            
            currentTreeCollider = null;
        }

        if (other == currentDamCollider)
        {
            isNearDam = false;
            if (currentDam == other.gameObject)
            {
                currentDam = null;
            }
            
            currentDamCollider = null;
        }
        else if (other.name.StartsWith("Water"))
        {
            agent.speed = 4f;
        }
    }

    private void InitRiverAndTrees()
    {
        if (world == null || world.all == null || world.all.Count == 0)
        {
            Debug.LogWarning("AIController: World is null or empty; cannot init river.");
            return;
        }

        if (world == null || world.all == null || world.all.Count == 0)
        {
            Debug.LogWarning("AIController: World is null or empty; cannot init river.");
            return;
        }

        // 1) Which hex did I spawn on (or nearest to)?
        Hex myHex = FindClosestHexToPosition(transform.position);
        if (myHex == null)
        {
            Debug.LogWarning("AIController: Could not find starting hex for this beaver.");
            return;
        }

        // 2) Which river is that hex on?
        try
        {
            myRiverHexes = world.FindRiverWithHex(myHex);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AIController: Hex not on any river: {e.Message}");
            return;
        }

        if (myRiverHexes == null || myRiverHexes.Count == 0)
        {
            Debug.LogWarning("AIController: myRiverHexes is empty after FindRiverWithHex.");
            return;
        }

        // 3) Collect all Log-children from landMesh on those hexes
        myRiverTrees.Clear();

        foreach (Hex hex in myRiverHexes)
        {
            if (hex == null || hex.landMesh == null) continue;

            Transform land = hex.landMesh.transform;

            // look for child objects whose name starts with "Log"
            foreach (Transform child in land)
            {
                if (child.name.StartsWith("Log", System.StringComparison.OrdinalIgnoreCase))
                {
                    myRiverTrees.Add(child.gameObject);
                    break; // one tree per tile, so we can stop after first match
                }
            }
        }

        Debug.Log($"AIController: Found {myRiverTrees.Count} river trees for this beaver.");

        myRiverDamHexes.Clear();
        foreach (Hex hex in myRiverHexes)
        {
            if (hex != null && hex.exitDam != null)
            {
                myRiverDamHexes.Add(hex);
            }
        }

        currentDamBuildIndex = 0;

        Debug.Log($"AIController: Found {myRiverDamHexes.Count} dam hexes on my river.");
    
    }

    private Hex FindClosestHexToPosition(Vector3 pos)
    {
        Hex closest = null;
        float minDist = Mathf.Infinity;

        foreach (Hex hex in world.all)
        {
            if (hex == null || hex.landMesh == null) continue;

            Vector3 hexPos = hex.landMesh.transform.position;
            float dist = Vector3.Distance(pos, hexPos);

            if (dist < minDist)
            {
                minDist = dist;
                closest = hex;
            }
        }

        return closest;
    }

    private GameObject FindNearestRiverTree()
    {
        if (myRiverTrees == null || myRiverTrees.Count == 0)
            return null;

        GameObject nearest = null;
        float minPathDist = Mathf.Infinity;

        foreach (GameObject tree in myRiverTrees)
        {
            if (tree == null || !tree.activeInHierarchy) continue;

            float pathDist = CalculatePathDistance(tree.transform.position);
            if (pathDist < minPathDist)
            {
                minPathDist = pathDist;
                nearest = tree;
            }
        }

        return nearest;
    }

        private void TryInitWorld()
    {
        if (worldInitialized) return;

        GameWorld gw = GameWorld.Instance();
        if (gw == null)
        {
            // GameWorld not ready yet
            return;
        }

        World w = gw.World();
        if (w == null)
        {
            // WorldBuilder hasn't finished ConstructMap() yet
            return;
        }

        world = w;
        InitRiverAndTrees();
        worldInitialized = true;
        Debug.Log("AIController: World initialized and river trees cached.");
    }
}