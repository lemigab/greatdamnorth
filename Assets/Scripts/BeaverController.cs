using UnityEngine;

public class BeaverController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    public bool isNearTree = false;
    public bool isNearDam = false;

    private GameObject currentDam = null;
    public GameObject currentTree = null;
    private GameObject branch;
    
    private bool _isHoldingLog = false;
    public bool IsHoldingLog
    {
        get { return _isHoldingLog; }
        set
        {
            _isHoldingLog = value;
            if (branch != null)
            {
                Debug.Log("Setting branch active: " + value);
                branch.SetActive(value);
            }
        }
    }

    private bool isPlayerBeaver = false;
    private bool isInEnemyZone = false;

    public virtual void Start()
    {
        //if (gameObject.CompareTag("Player"))
        //{
        //    isPlayerBeaver = true;
        //}

        branch = transform.Find("Branch").gameObject;
        IsHoldingLog = false;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter: " + other.gameObject.name);
        if (other.gameObject.name.StartsWith("Tree"))
        {
            isNearTree = true;
            currentTree = other.gameObject;
            Debug.Log("Near tree: " + currentTree.name);
        }
        if (other.gameObject.name.StartsWith("Dam"))
        {
            isNearDam = true;
            currentDam = other.gameObject;
            Debug.Log("Near dam: " + currentDam.name + " " + currentDam.GetComponent<BeaverDam>().Level().ToString());
        }
        if (other.gameObject.name.StartsWith("Land"))
        {
            moveSpeed = 2f;
        }
        if (other.gameObject.name.StartsWith("Water"))
        {
            //Debug.Log("On water: " + other.gameObject.name);
            moveSpeed = 5f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.StartsWith("Tree"))
        {
            isNearTree = false;
            currentTree = null;
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
            Debug.Log("On land: " + other.gameObject.name);
            moveSpeed = 2f;
        }
    }

    public virtual void Move(Vector3 targetDirection)
    {
        transform.position += targetDirection * (moveSpeed * Time.deltaTime);

        var rotationDirection = targetDirection;
        var rotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    public void Chew()
    {
        /*
        if (isNearTree && !IsHoldingLog && currentTree != null)
        {
            //Debug.Log("Chew tree: " + currentTree.name);
            currentTree.SetActive(false);
            if (isPlayerBeaver) SandboxGlobal.GetInstance().PlayerHoldingLog = true;
            else SandboxGlobal.GetInstance().EnemyHoldingLog = true;
            IsHoldingLog = true; // Automatically sets branch active
            currentTree = null;
        }
        */
        IsHoldingLog = true;
    }

    public void BuildDam()
    {
        if (currentDam != null && IsHoldingLog)
        {
           Debug.Log("Build dam: " + currentDam.name);
           currentDam.GetComponent<BeaverDam>().Increment();
           IsHoldingLog = false;
        }
    }

    public void BreakDam()
    {

        if (currentDam != null)
        {
            Debug.Log("Break dam: " + currentDam.name);
            currentDam.GetComponent<BeaverDam>().Decrement();
            IsHoldingLog = true;
        }
    }

    // TODO: Implement lodge building later
    public void BuildLodge()
    {
        //Debug.Log("Build lodge");
    }
} 