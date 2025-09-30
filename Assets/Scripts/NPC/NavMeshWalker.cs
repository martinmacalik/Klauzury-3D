// NavMeshWanderer.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshWanderer : MonoBehaviour
{
    [Header("Wander")]
    public float roamRadius = 20f;
    public bool recenterAroundCurrent = true;

    [Header("Pauses")]
    public float minWaitAtPoint = 0.5f;
    public float maxWaitAtPoint = 2.0f;

    [Header("Agent")]
    public float moveSpeed = 2.0f;
    public float angularSpeed = 360f;
    public float acceleration = 8f;
    public float stoppingDistance = 0.2f;

    [Header("Reliability")]
    [Tooltip("If barely moving for this long while it should be, repath.")]
    public float stuckSeconds = 3f;
    [Tooltip("Max distance NavMesh.SamplePosition can snap a pick.")]
    public float sampleMaxDistance = 2.0f;

    [Header("Areas (optional)")]
    [Tooltip("Restrict movement to these NavMesh areas (e.g. \"Walkable\", \"Sidewalk\"). Leave empty for all.")]
    public string[] allowedAreaNames;

    private NavMeshAgent agent;
    private Vector3 basePoint;
    private int areaMask = NavMesh.AllAreas;
    private float stuckTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;

        basePoint = transform.position;

        // Build area mask if names provided
        if (allowedAreaNames != null && allowedAreaNames.Length > 0)
        {
            areaMask = 0;
            foreach (var name in allowedAreaNames)
            {
                int idx = NavMesh.GetAreaFromName(name);
                if (idx >= 0) areaMask |= (1 << idx);
                else Debug.LogWarning($"[NavMeshWanderer] Area \"{name}\" not found.");
            }
            if (areaMask == 0) areaMask = NavMesh.AllAreas;
        }
    }

    void OnEnable() => StartCoroutine(Run());

    IEnumerator Run()
    {
        // Wait until the agent is placed on a NavMesh
        yield return new WaitUntil(() => TryEnsureOnNavMesh());

        // Kick off first move
        yield return MoveToNext();

        while (enabled)
        {
            if (agent.isOnNavMesh)
            {
                // Arrived?
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    float wait = Random.Range(minWaitAtPoint, maxWaitAtPoint);
                    yield return new WaitForSeconds(wait);
                    yield return MoveToNext();
                }

                // Stuck detection
                bool shouldBeMoving = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f;
                bool barelyMoving = agent.velocity.sqrMagnitude < 0.01f;
                if (shouldBeMoving && barelyMoving)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= stuckSeconds)
                    {
                        stuckTimer = 0f;
                        yield return MoveToNext(forceRecenter:true);
                    }
                }
                else stuckTimer = 0f;
            }

            yield return null;
        }
    }

    IEnumerator MoveToNext(bool forceRecenter = false)
    {
        if (!agent.isOnNavMesh)
        {
            TryEnsureOnNavMesh();
            if (!agent.isOnNavMesh) yield break;
        }

        Vector3 center = (recenterAroundCurrent || forceRecenter) ? transform.position : basePoint;

        // Try up to 10 random picks within radius
        for (int i = 0; i < 10; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = center + new Vector3(rnd.x, 0f, rnd.y);

            if (NavMesh.SamplePosition(candidate, out var hit, sampleMaxDistance, areaMask))
            {
                var path = new NavMeshPath();
                if (NavMesh.CalculatePath(agent.transform.position, hit.position, areaMask, path) &&
                    path.status != NavMeshPathStatus.PathInvalid)
                {
                    agent.SetPath(path);
                    yield break;
                }
            }
        }

        // Couldn’t find a good spot now; retry soon
        yield return new WaitForSeconds(0.5f);
    }

    bool TryEnsureOnNavMesh()
    {
        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, areaMask) ||
            NavMesh.SamplePosition(transform.position, out hit, 10.0f, areaMask))
        {
            agent.Warp(hit.position);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = recenterAroundCurrent ? transform.position : (Application.isPlaying ? basePoint : transform.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, roamRadius);
    }
#endif
}
