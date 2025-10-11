using UnityEngine;

public class BeaverController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public bool isNearTree = false;
    public bool isHoldingLog = false;
    public bool isNearDam = false;
    private GameObject currentDam = null;
    public GameObject currentTree = null;

    private bool isPlayerBeaver = false;
    private bool isInEnemyZone = false;

    void Start() {
        if (gameObject.CompareTag("Player")) {
            isPlayerBeaver = true;
        }
    }

    void OnTriggerEnter(Collider other) {
        Debug.Log("OnTriggerEnter: " + other.gameObject.name);
        if (other.gameObject.name.StartsWith("Tree")) {
            isNearTree = true;
            currentTree = other.gameObject;
            Debug.Log("Near tree: " + currentTree.name);
        }
        if (other.gameObject.name.StartsWith("Dam")) {
            isNearDam = true;
            currentDam = other.gameObject;
            Debug.Log("Near dam: " + currentDam.name);
        }
        if (other.gameObject.name.StartsWith("Land")) {
            if (other.gameObject.CompareTag("EnemyZone") && isPlayerBeaver) {
                Debug.Log("In Enemy Zone");
                isInEnemyZone = true;
            }
            if (other.gameObject.CompareTag("PlayerZone") && !isPlayerBeaver) {
                Debug.Log("In Player Zone");
                isInEnemyZone = true;
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.gameObject.name.StartsWith("Tree")) {
            isNearTree = false;
            currentTree = null;
            Debug.Log("Not near tree");
        }
        if (other.gameObject.name.StartsWith("Dam")) {
            isNearDam = false;
            currentDam = null;
            Debug.Log("Not near dam");
        }
        if (other.gameObject.CompareTag("EnemyZone") && isPlayerBeaver) {
            isInEnemyZone = false;
            Debug.Log("Not in Enemy Zone");
        }
        if (other.gameObject.CompareTag("PlayerZone") && !isPlayerBeaver) {
            isInEnemyZone = false;
            Debug.Log("Not in Enemy Zone");
        }
    }

    public void Move(Vector3 targetDirection) {
        transform.position += targetDirection * (moveSpeed * Time.deltaTime);

        var rotationDirection = Quaternion.Euler(-90,0,0) * targetDirection;
        var rotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(-90,0,0);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    public void Chew() {
        if (isNearTree && !isHoldingLog && currentTree != null) {
            Debug.Log("Chew tree: " + currentTree.name);
            currentTree.SetActive(false);
            Transform mouthLog = transform.Find("MouthLog");
            Debug.Log("MouthLog: " + mouthLog.gameObject.name);
            mouthLog.gameObject.GetComponent<MeshRenderer>().enabled = true;
            isHoldingLog = true;
        }
    }

    public void BuildDam() {
        if (isHoldingLog && isInEnemyZone && currentDam != null) {
            Debug.Log("Build dam: " + currentDam.name);
            Transform mouthLog = transform.Find("MouthLog");
            mouthLog.gameObject.GetComponent<MeshRenderer>().enabled = false;
            if (isPlayerBeaver) {
                SandboxGlobal.GetInstance().EnemyDamLevel++;
                Debug.Log("Enemy dam level: " + SandboxGlobal.GetInstance().EnemyDamLevel);
            } else {
                SandboxGlobal.GetInstance().PlayerDamLevel++;
            }
            isHoldingLog = false;
        }
    }

    public void BreakDam() {
        if (isNearDam && !isInEnemyZone && currentDam.gameObject.GetComponent<MeshRenderer>().enabled) {
            Debug.Log("Break dam");
            if (isPlayerBeaver) {
                SandboxGlobal.GetInstance().EnemyDamLevel--;
            } else {
                SandboxGlobal.GetInstance().PlayerDamLevel--;
            }
        }
    }

    // TODO: Implement lodge building later
    public void BuildLodge() {
        Debug.Log("Build lodge");
    }
}
