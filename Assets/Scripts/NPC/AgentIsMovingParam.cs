using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentIsMovingParam : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Animator with a Bool parameter you use to switch Idle/Walk.")]
    public Animator animator;
    [Tooltip("Name of the Bool parameter on the Animator.")]
    public string boolParam = "isMoving";

    [Header("Tuning")]
    [Tooltip("Speed at/above which we consider the agent moving (m/s).")]
    public float speedThreshold = 0.05f;
    [Tooltip("Extra buffer beyond stoppingDistance to count as 'not arrived' (m).")]
    public float distanceEpsilon = 0.05f;

    private NavMeshAgent agent;
    private int paramHash;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        paramHash = Animator.StringToHash(boolParam);
    }

    void Update()
    {
        if (!animator || agent == null) return;

        bool moving = false;

        if (agent.isOnNavMesh)
        {
            // If we have a path and haven't reached our stopping zone, or velocity is non-trivial, we're "moving".
            bool shouldBeMoving = agent.hasPath && agent.remainingDistance > (agent.stoppingDistance + distanceEpsilon);
            bool actuallyMoving = agent.velocity.sqrMagnitude > (speedThreshold * speedThreshold);
            moving = (shouldBeMoving || actuallyMoving) && !agent.pathPending;
        }
        else
        {
            // Fallback: use raw rigidbody/transform motion if not on NavMesh.
            moving = agent.velocity.sqrMagnitude > (speedThreshold * speedThreshold);
        }

        animator.SetBool(paramHash, moving);
    }
}