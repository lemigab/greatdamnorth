using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BeaverController : MonoBehaviour
{

    public Rigidbody rb;

    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

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

    public GameObject currentDam = null;
    public GameObject currentLog = null;
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
                Debug.Log("Setting branch active: " + value);
                branch.SetActive(value);
            }
        }
    }

    // private bool isPlayerBeaver = false;
    // private bool isInEnemyZone = false;

    public virtual void Start()
    {
        //if (gameObject.CompareTag("Player"))
        //{
        //    isPlayerBeaver = true;
        //}
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
            Debug.Log("Near log: " + currentLog.name);
        }
        if (other.gameObject.name.StartsWith("Dam"))
        {
            isNearDam = true;
            currentDam = other.gameObject;
            Debug.Log("Near dam: " + currentDam.name + " " + currentDam.GetComponent<BeaverDam>().Level().ToString());
        }
        if (other.gameObject.name.StartsWith("Water"))
        {
            moveSpeed = 8f;
            //Debug.Log("On water: " + other.gameObject.name + " " + moveSpeed.ToString());
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
        if (other.gameObject.name.StartsWith("Water"))
        {
            moveSpeed = 4f;
            //Debug.Log("On land: " + other.gameObject.name + " " + moveSpeed.ToString());
        }
    }

    public virtual void Move(Vector3 targetDirection)
    {
        /*
        transform.position += targetDirection * (moveSpeed * Time.deltaTime);

        var rotationDirection = targetDirection;
        var rotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
        */
        Vector3 moveDirection = targetDirection * moveSpeed;
        rb.MovePosition(transform.position + moveDirection * Time.deltaTime);

        var rotation = Quaternion.LookRotation(targetDirection);
        rb.MoveRotation(Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime));
    }

    public void Chew()
    {
        if (isNearLog && !isHoldingBranch && currentLog != null)
        {
            currentLog.SetActive(false);
            isHoldingBranch = true;
            currentLog = null;
        }
    }

    public void BuildDam()
    {
        // Only start if we *currently* can build
        if (currentDam == null || !isHoldingBranch)
            return;

        // Capture the dam we’re building on right now
        BeaverDam dam = currentDam.GetComponent<BeaverDam>();
        if (dam == null)
            return;

        StartCoroutine(BuildDamSequence(dam));
    }
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

            Debug.Log($"AI jump: {name} baseOffset={agent.baseOffset}");
        }

        // Wait while the AI is "in the air" before dam gets bigger
        yield return new WaitForSeconds(hangTime);

        // Now actually grow the dam and consume the log
        if (dam != null)
        {
            Debug.Log("Build dam: " + dam.gameObject.name);
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

    public void BreakDam()
    {

        if (currentDam != null)
        {
            Debug.Log("Break dam: " + currentDam.name);
            currentDam.GetComponent<BeaverDam>().Decrement();
            isHoldingBranch = true;
        }
    }

    // TODO: Implement lodge building later
    public void BuildLodge()
    {
        //Debug.Log("Build lodge");
    }
} 