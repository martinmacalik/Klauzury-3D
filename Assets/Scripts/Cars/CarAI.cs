using UnityEngine;

public class CarAI : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 5f;
    public float turnSpeed = 5f;
    public float waypointRadius = 1f;

    [HideInInspector] public int currentIndex = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];

        // Direction to the next waypoint
        Vector3 dir = (target.position - transform.position).normalized;

        // Rotate smoothly toward the waypoint
        Quaternion lookRotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

        // Move forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Check if close enough to switch to the next waypoint
        if (Vector3.Distance(transform.position, target.position) < waypointRadius)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length; // loop back to 0
        }
    }
}