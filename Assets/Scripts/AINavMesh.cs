using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AINavMesh : MonoBehaviour
{
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Optional: area costs so water is preferred over land
        int water = NavMesh.GetAreaFromName("Water");
        int land  = NavMesh.GetAreaFromName("Land");

        if (water >= 0) agent.SetAreaCost(water, 1f);  // cheap = preferred
        if (land  >= 0) agent.SetAreaCost(land, 3f);   // expensive = avoid if possible
    }

    public void MoveTo(Vector3 destination)
    {
        if (!agent.enabled) return;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (!agent.enabled) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    public bool HasReached(float tolerance = 0.1f)
    {
        if (!agent.enabled || agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance + tolerance;
    }
}