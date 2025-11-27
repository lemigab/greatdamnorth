using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using WorldUtil;
using NUnit.Framework;
using System.Collections.Generic;

public class BeaverController : MonoBehaviour
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

    private bool _isHoldingBranch = false;
    public bool isHoldingBranch
    {
        get { return _isHoldingBranch; }
        set
        {
            _isHoldingBranch = value;
            if (branch != null)
            {
                //Debug.Log("Setting branch active: " + value);
                branch.SetActive(value);
            }
        }
    }


    public SyrupFarm GetHomeFarm()
    {
        World w = GameWorld.Instance().World();
        if (syrupFarmId >= w.syrupFarms.Count)
            throw new System.Exception("Not a valid target farm");
        return w.syrupFarms[syrupFarmId];
    }


    public virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        branch = transform.Find("Branch").gameObject;
        isHoldingBranch = false;
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
        rb.MovePosition(transform.position + moveDirection * Time.deltaTime);

        var rotation = Quaternion.LookRotation(targetDirection);
        rb.MoveRotation(Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime));
    }

    public bool ChewLog()
    {
        if (isNearLog && !isHoldingBranch && currentLog != null)
        {
            currentLog.SetActive(false);
            isHoldingBranch = true;
            currentLog = null;
            return true;
        }
        return false;
    }

    public bool BuildDam()
    {
        // Only start if we *currently* can build
        if (currentDam == null || !isHoldingBranch)
            return false;

        // Capture the dam we’re building on right now
        BeaverDam dam = currentDam.GetComponent<BeaverDam>();
        if (dam == null)
            return false;

        // Remove this part if you are putting the corountine back in
        dam.Increment();
        Vector3 offset = new(0f, dam.OffsetPerLevel() + 0.01f, 0f);
        World w = GameWorld.Instance().World();
        List<Hex> ups = w.UpstreamFrom(w.FindHexWithDam(dam), true);
        foreach (BeaverController b in GameWorld.Instance().allBeavers)
        {
            foreach (Hex h in ups)
            {
                Vector3 bPos = b.gameObject.transform.position;
                if (h.landMesh.GetComponent<MeshRenderer>().bounds.Contains(bPos))
                    b.gameObject.transform.position = bPos + offset;
            }
        }

        isHoldingBranch = false;

        // StartCoroutine(BuildDamSequence(dam));
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
        if (dam != null && isHoldingBranch &&
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

        isHoldingBranch = false;

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

    public bool BreakDam()
    {

        if (currentDam != null && !isHoldingBranch)
        {
            //Debug.Log("Break dam: " + currentDam.name);
            currentDam.GetComponent<BeaverDam>().Decrement();
            isHoldingBranch = true;
            return true;
        }
        return false;
    }


    public bool BuildLodge()
    {
        // Only start if we *currently* can build
        if (currentLodge == null || !isHoldingBranch)
            return false;

        // Capture the lodge we’re building on right now
        BeaverLodge l = currentLodge.GetComponent<BeaverLodge>();
        if (l == null) return false;

        // Cancel if insufficient water level
        World w = GameWorld.Instance().World();
        if (w.FindHexWithLodge(l).WaterLevel() == 0) return false;

        // Build the lodge
        l.Build(GetHomeFarm());
        isHoldingBranch = false;
        return true;
    }

    public bool BuildMound()
    {
        // Only start if we *currently* can build
        if (currentMound == null || !isHoldingBranch)
            return false;

        // Capture the mound we’re building on right now
        BeaverMound m = currentMound.GetComponent<BeaverMound>();
        if (m == null) return false;

        // Build the mound
        m.Build(GetHomeFarm());
        isHoldingBranch = false;
        return true;
    }

    public bool BreakMound()
    {
        if (currentMound != null && !isHoldingBranch)
        {
            currentMound.GetComponent<BeaverMound>().Dismantle();
            isHoldingBranch = true;
            return true;
        }
        return false;
    }
}