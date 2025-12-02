using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using WorldUtil;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;

public class BeaverController : NetworkBehaviour
{
    public int syrupFarmId;

    public Rigidbody rb;

    public float moveSpeed = 10f;
    public float rotationSpeed = 20f;

    private bool _isNearLog = false;
    public bool isNearLog
    {
        get { return _isNearLog; }
        set { _isNearLog = value; }
    }

    private bool _isNearDam = false;
    public bool isNearDam
    {
        get { return _isNearDam; }
        set { _isNearDam = value; }
    }

    private bool _isNearMound = false;
    public bool isNearMound
    {
        get { return _isNearMound; }
        set { _isNearMound = value; }
    }

    private bool _isNearLodge = false;
    public bool isNearLodge
    {
        get { return _isNearLodge; }
        set { _isNearLodge = value; }
    }

    public GameObject currentDam = null;
    public GameObject currentLog = null;
    public GameObject currentMound = null;
    public GameObject currentLodge = null;
    private GameObject branch;

    // NetworkVariable to sync branch visibility across all clients
    public NetworkVariable<bool> isHoldingBranch = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public SyrupFarm GetHomeFarm()
    {
        World w = GameWorld.Instance().World();
        if (syrupFarmId >= w.syrupFarms.Count)
            throw new System.Exception("Not a valid target farm");
        return w.syrupFarms[syrupFarmId];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Give beaver a unique name based on client ID and ownership
        if (IsOwner)
        {
            gameObject.name = $"PlayerBeaver-{OwnerClientId}";
        }
        else
        {
            gameObject.name = $"Beaver-{OwnerClientId}-{NetworkObjectId}";
        }
        
        // Subscribe to NetworkVariable changes to update branch visibility
        isHoldingBranch.OnValueChanged += OnBranchVisibilityChanged;
        
        // Set initial branch visibility based on current NetworkVariable value
        if (branch != null)
        {
            branch.SetActive(false);
        }

        GameWorld.Instance().allBeavers.Add(this);
        
        Debug.Log($"[BeaverController] OnNetworkSpawn - Name: {gameObject.name}, Position: {transform.position}, IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}, NetworkObjectId: {NetworkObjectId}, OwnerClientId: {OwnerClientId}");
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from NetworkVariable changes
        isHoldingBranch.OnValueChanged -= OnBranchVisibilityChanged;
        GameWorld.Instance().allBeavers.Remove(this);
        base.OnNetworkDespawn();
    }

    private void OnBranchVisibilityChanged(bool previousValue, bool newValue)
    {
        // Update branch visibility when NetworkVariable changes (syncs to all clients)
        if (branch != null)
        {
            branch.SetActive(newValue);
        }
        Debug.Log($"[BeaverController] Branch visibility changed: {newValue} for {gameObject.name}");
    }

    public virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.isKinematic = false;
        //rb.useGravity = true;

        branch = transform.Find("Branch").gameObject;
        if (branch != null)
        {
            // Set initial visibility (will be synced via NetworkVariable if networked)
            branch.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter: " + other.gameObject.name);
        if (other.gameObject.name.StartsWith("Log"))
        {
            isNearLog = true;
            currentLog = other.gameObject;
            //Debug.Log("Near log: " + currentLog.name);
        }
        if (other.gameObject.name.StartsWith("Dam"))
        {
            isNearDam = true;
            currentDam = other.gameObject;
            //Debug.Log("Near dam: " + currentDam.name + " " + currentDam.GetComponent<BeaverDam>().Level().ToString());
        }
        if (other.gameObject.name.StartsWith("Pointer"))
        {
            isNearDam = true;
            currentDam = other.transform.parent.gameObject;
            //Debug.Log("Near dam: " + currentDam.name + " " + currentDam.GetComponent<BeaverDam>().Level().ToString());
        }
        if (other.gameObject.name.StartsWith("Water"))
        {
            moveSpeed = 8f;
            //Debug.Log("On water: " + other.gameObject.name + " " + moveSpeed.ToString());
        }
        if (other.gameObject.name.StartsWith("Mound"))
        {
            isNearMound = true;
            currentMound = other.gameObject;
        }
        if (other.gameObject.name.StartsWith("Lodge"))
        {
            isNearLodge = true;
            currentLodge = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.StartsWith("Log"))
        {
            isNearLog = false;
            currentLog = null;
            // Debug.Log("Not near tree");
        }
        if (other.gameObject.name.StartsWith("Dam"))
        {
            isNearDam = false;
            currentDam = null;
            //Debug.Log("Not near dam");
        }
        if (other.gameObject.name.StartsWith("Pointer"))
        {
            isNearDam = false;
            currentDam = null;
            //Debug.Log("Not near dam");
        }
        if (other.gameObject.name.StartsWith("Water"))
        {
            moveSpeed = 4f;
            //Debug.Log("On land: " + other.gameObject.name + " " + moveSpeed.ToString());
        }
        if (other.gameObject.name.StartsWith("Mound"))
        {
            isNearMound = false;
            currentMound = null;
        }
        if (other.gameObject.name.StartsWith("Lodge"))
        {
            isNearLodge = false;
            currentLodge = null;
        }
    }

    public virtual void Move(Vector3 targetDirection)
    {
        Vector3 moveDirection = targetDirection * moveSpeed;
        // Use fixedDeltaTime since Move() is called from FixedUpdate context
        rb.MovePosition(transform.position + moveDirection * Time.fixedDeltaTime);

        var rotation = Quaternion.LookRotation(targetDirection);
        rb.MoveRotation(Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.fixedDeltaTime));
    }

    [Rpc(SendTo.Server)]
    public void ChewLogServerRpc()
    {
        Debug.Log("BeaverController ChewLogServerRpc: isNearLog=" + isNearLog + ", isHoldingBranch=" + isHoldingBranch.Value + ", currentLog=" + currentLog);
        if (isNearLog && !isHoldingBranch.Value && currentLog != null)
        {
            // Despawn the log so it disappears on all clients
            NetworkObject logNetObj = currentLog.GetComponent<NetworkObject>();
            if (logNetObj != null && logNetObj.IsSpawned)
            {
                Debug.Log($"Despawning log: {logNetObj.name}, NetworkObjectId: {logNetObj.NetworkObjectId}");
                logNetObj.Despawn(true); // Destroy the GameObject immediately
                
                Debug.Log($"Log {logNetObj.name} despawned. IsSpawned after despawn: {logNetObj.IsSpawned}");
            }
            else
            {
                Debug.LogWarning($"Cannot despawn log: logNetObj={logNetObj}, IsSpawned={logNetObj?.IsSpawned}");
            }
            isHoldingBranch.Value = true;
            currentLog = null;
        }
    }

    // Keep this for backwards compatibility, but it now calls ServerRpc
    public bool ChewLog()
    {
        Debug.Log("BeaverController ChewLog: isNearLog=" + isNearLog + ", isHoldingBranch=" + isHoldingBranch.Value + ", currentLog=" + currentLog);
        if (isNearLog && !isHoldingBranch.Value && currentLog != null)
        {
            Debug.Log("BeaverController ChewLog: calling ChewLogServerRpc");
            ChewLogServerRpc();
            return true;
        }
        return false;
    }

    [Rpc(SendTo.Server)]
    public void BuildDamServerRpc()
    {
        BeaverDam dam = currentDam.GetComponent<BeaverDam>();
        if (dam == null)
            return;

        // Remove this part if you are putting the corountine back in
        dam.Increment();
        Vector3 offset = new(0f, dam.OffsetPerLevel() + 0.01f, 0f);
        
        // Safely get world and upstream hexes
        if (GameWorld.Instance() == null)
            return;
            
        World w = GameWorld.Instance().World();
        if (w == null)
            return;
            
        Hex damHex = w.FindHexWithDam(dam);
        if (damHex == null)
            return;
            
        List<Hex> ups = w.UpstreamFrom(damHex, true);
        if (ups == null)
            return;
        
        // Check if allBeavers is null or empty
        if (GameWorld.Instance().allBeavers != null)
        {
            foreach (BeaverController b in GameWorld.Instance().allBeavers)
            {
                // Skip null beavers or destroyed GameObjects
                if (b == null || b.gameObject == null)
                    continue;
                    
                foreach (Hex h in ups)
                {
                    if (h?.landMesh == null)
                        continue;
                        
                    Vector3 bPos = b.gameObject.transform.position;
                    MeshRenderer mr = h.landMesh.GetComponent<MeshRenderer>();
                    if (mr != null && mr.bounds.Contains(bPos))
                    {
                        b.gameObject.transform.position = bPos + offset;
                    }
                }
            }
        }

        isHoldingBranch.Value = false;

        // StartCoroutine(BuildDamSequence(dam));
    }

    public bool BuildDam()
    {
        // Only start if we *currently* can build
        if (currentDam == null || !isHoldingBranch.Value)
            return false;

        // Capture the dam we’re building on right now
        BuildDamServerRpc();

        return true;
    }

    // UNUSED for A4 since there are no AIs
    private IEnumerator BuildDamSequence(BeaverDam dam)
    {
        NavMeshAgent agent = null;
        float originalOffset = 0f;
        float jumpHeight = 4f;     // how high above the dam
        float hangTime = 0.2f;     // how long before dam grows
        float fallTime = 0.5f;     // how long to ease back down
        bool isAI = false;

        // If this beaver is an AI (has NavMeshAgent), position it above the dam
        if (dam != null && isHoldingBranch.Value &&
            TryGetComponent<NavMeshAgent>(out agent) && agent.enabled)
        {
            isAI = true;
            originalOffset = agent.baseOffset;

            Vector3 damPos = dam.transform.position;

            // Snap the agent to the dam's XZ on the NavMesh
            if (NavMesh.SamplePosition(damPos, out var hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                agent.Warp(damPos);
            }

            // Visually lift it above the dam
            agent.baseOffset = originalOffset + jumpHeight;

            if (rb != null)
            {
                // keep Rigidbody aligned with agent
                rb.position = agent.nextPosition + Vector3.up * agent.baseOffset;
            }

            //Debug.Log($"AI jump: {name} baseOffset={agent.baseOffset}");
        }

        // Wait while the AI is "in the air" before dam gets bigger
        yield return new WaitForSeconds(hangTime);

        // Now actually grow the dam and consume the log
        if (dam != null)
        {
            //Debug.Log("Build dam: " + dam.gameObject.name);
            dam.Increment();
        }

        isHoldingBranch.Value = false;

        // Smoothly drop AI back down to normal height
        if (isAI && agent != null)
        {
            float elapsed = 0f;
            float startOffset = agent.baseOffset;
            float targetOffset = originalOffset;

            while (elapsed < fallTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallTime);
                agent.baseOffset = Mathf.Lerp(startOffset, targetOffset, t);

                if (rb != null)
                {
                    rb.position = agent.nextPosition + Vector3.up * agent.baseOffset;
                }

                yield return null;
            }

            agent.baseOffset = targetOffset;
        }
    }

    [Rpc(SendTo.Server)]
    public void BreakDamServerRpc()
    {
        BeaverDam dam = currentDam.GetComponent<BeaverDam>();
        if (dam == null)
            return;
        dam.Decrement();
        isHoldingBranch.Value = true;
    }

    public bool BreakDam()
    {

        if (currentDam != null && !isHoldingBranch.Value)
        {
            //Debug.Log("Break dam: " + currentDam.name);
            BreakDamServerRpc();
            return true;
        }
        return false;
    }


    [Rpc(SendTo.Server)]
    public void BuildLodgeServerRpc()
    {
        BeaverLodge l = currentLodge.GetComponent<BeaverLodge>();
        if (l == null) return;

        // Cancel if insufficient water level
        World w = GameWorld.Instance().World();
        if (w.FindHexWithLodge(l).WaterLevel() == 0) return;

        // Build the lodge
        l.Build(GetHomeFarm());
        if (IsServer)
        {
            isHoldingBranch.Value = false;
        }
    }


    public bool BuildLodge()
    {
        // Only start if we *currently* can build
        if (currentLodge == null || !isHoldingBranch.Value)
            return false;

        BuildLodgeServerRpc();
        return true;
    }

    public bool BuildMound()
    {
        // Only start if we *currently* can build
        if (currentMound == null || !isHoldingBranch.Value)
            return false;

        // Capture the mound we’re building on right now
        BeaverMound m = currentMound.GetComponent<BeaverMound>();
        if (m == null) return false;

        // Build the mound
        m.Build(GetHomeFarm());
        if (IsServer)
        {
            isHoldingBranch.Value = false;
        }
        return true;
    }

    public bool BreakMound()
    {
        if (currentMound != null && !isHoldingBranch.Value)
        {
            currentMound.GetComponent<BeaverMound>().Dismantle();
            if (IsServer)
            {
                isHoldingBranch.Value = true;
            }
            return true;
        }
        return false;
    }
}