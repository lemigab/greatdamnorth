using UnityEngine;

public class PreviewBoat : MonoBehaviour
{
    private Vector3 targetPos = new(0f, 0f, 0f);

    private bool travelToTarget = false;

    private const float targetBuffer = 1f;

    public void SetTarget(Vector3 pos)
    {
        targetPos = pos;
        travelToTarget = true;
    }

    public void FixedUpdate()
    {

    }
}
