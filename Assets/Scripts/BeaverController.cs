using UnityEngine;

public class BeaverController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public bool isNearTree = false;
    public GameObject currentTree = null;

    void OnTriggerEnter(Collider other) {
        Debug.Log("OnTriggerEnter: " + other.gameObject.name);
        if (other.gameObject.name.StartsWith("Tree")) {
            isNearTree = true;
            currentTree = other.gameObject;
            Debug.Log("Near tree: " + currentTree.name);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.gameObject.name.StartsWith("Tree")) {
            isNearTree = false;
            currentTree = null;
            Debug.Log("Not near tree");
        }
    }

    public void Move(Vector3 targetDirection) {
        transform.position += targetDirection * (moveSpeed * Time.deltaTime);

        var rotationDirection = Quaternion.Euler(-90,0,0) * targetDirection;
        var rotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(-90,0,0);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    public void Chew() {
        if (isNearTree) {
            Debug.Log("Chew tree: " + currentTree.name);
        }
    }

    public void buildDam() {
        Debug.Log("Build dam");
    }

    public void buildLodge() {
        Debug.Log("Build lodge");
    }

    public void breakDam() {
        Debug.Log("Break dam");
    }
}
