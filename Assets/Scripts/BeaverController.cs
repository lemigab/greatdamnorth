using UnityEngine;

public class BeaverController : MonoBehaviour
{
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

    private GameObject currentDam = null;
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

    private bool isPlayerBeaver = false;
    private bool isInEnemyZone = false;

    public virtual void Start()
    {
        //if (gameObject.CompareTag("Player"))
        //{
        //    isPlayerBeaver = true;
        //}

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
        if (isNearLog && !isHoldingBranch && currentLog != null)
        {
            currentLog.SetActive(false);
            isHoldingBranch = true;
            currentLog = null;
        }
    }

    public void BuildDam()
    {
        if (currentDam != null && isHoldingBranch)
        {
           Debug.Log("Build dam: " + currentDam.name);
           currentDam.GetComponent<BeaverDam>().Increment();
           isHoldingBranch = false;
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