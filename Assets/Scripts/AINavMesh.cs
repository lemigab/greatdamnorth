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
        if (land  >= 0) agent.SetAreaCost(land, 100f);   // expensive = avoid if possible
    }

    public void MoveTo(Vector3 destination)
    {
        if (!agent.enabled) return;
        
        // Validate destination is on NavMesh
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(destination, out hit, 5f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"AINavMesh: Destination {destination} is not on NavMesh!");
            return;
        }
        
        // Check if path exists
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path))
        {
            Debug.LogWarning($"AINavMesh: No valid path to {hit.position}");
            return;
        }
        
        // Check if path is complete (not partial)
        if (path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"AINavMesh: Path to {hit.position} is incomplete (status: {path.status})");
            // Still try to move, but warn
        }
        
        agent.isStopped = false;
        agent.SetDestination(hit.position);
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
        
        // Check if path is valid
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }
        
        // Check if we've reached the destination
        return agent.remainingDistance <= agent.stoppingDistance + tolerance;
    }
    
    /// <summary>
    /// Gets the NavMeshAgent component
    /// </summary>
    public NavMeshAgent GetAgent()
    {
        return agent;
    }
    
    /// <summary>
    /// Checks if the agent has a valid path
    /// </summary>
    public bool HasValidPath()
    {
        if (!agent.enabled) return false;
        return agent.pathStatus == NavMeshPathStatus.PathComplete;
    }
}