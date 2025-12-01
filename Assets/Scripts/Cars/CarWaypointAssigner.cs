using UnityEngine;

/// <summary>
/// Helper script to assign waypoints to a car's AI driver in the scene.
/// Attach this to a GameObject and assign waypoints and the car's AI driver in the Inspector.
/// </summary>
public class CarWaypointAssigner : MonoBehaviour
{
    [Header("Assign These in Inspector")]
    [Tooltip("The AI driver component of the car you want to give waypoints to")]
    public CarAIDriver targetCar;
    
    [Tooltip("The waypoint transforms this car should follow")]
    public Transform[] waypointsToAssign;
    
    [Header("Options")]
    [Tooltip("Assign waypoints automatically on Awake")]
    public bool assignOnAwake = true;
    
    void Awake()
    {
        if (assignOnAwake)
        {
            AssignWaypoints();
        }
    }
    
    [ContextMenu("Assign Waypoints Now")]
    public void AssignWaypoints()
    {
        if (targetCar == null)
        {
            Debug.LogError("[CarWaypointAssigner] No target car assigned!", this);
            return;
        }
        
        if (waypointsToAssign == null || waypointsToAssign.Length == 0)
        {
            Debug.LogWarning($"[CarWaypointAssigner] No waypoints to assign to {targetCar.name}", this);
            return;
        }
        
        targetCar.waypoints = waypointsToAssign;
        Debug.Log($"[CarWaypointAssigner] Assigned {waypointsToAssign.Length} waypoints to {targetCar.name}", this);
    }
}

