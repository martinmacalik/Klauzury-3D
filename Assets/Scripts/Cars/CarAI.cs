using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    [HideInInspector] public int currentIndex = 0;
    [Tooltip("How close to a waypoint before switching to the next.")]
    public float waypointSwitchRadius = 1.0f;

    [Header("Movement")]
    public float speed = 5f;          // cruise speed (m/s)
    public float turnSpeed = 5f;      // turn rate toward waypoint
    public float accel = 10f;         // accelerate toward speed
    public float brakeAccel = 15f;    // brake toward 0

    private float currentSpeed = 0f;
    public float CurrentSpeed => currentSpeed;

    // Track active front-back contacts
    private readonly HashSet<CarAI> frontBackContacts = new HashSet<CarAI>();
    public void NotifyFrontBackEnter(CarAI other) => frontBackContacts.Add(other);
    public void NotifyFrontBackExit(CarAI other)  => frontBackContacts.Remove(other);
    bool HasFrontBackContact => frontBackContacts.Count > 0;

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // ---- steer toward current waypoint ----
        Transform target = waypoints[currentIndex];
        Vector3 toTarget = target.position - transform.position; 
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        // ---- advance waypoint if close ----
        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTar = new Vector3(target.position.x, 0f, target.position.z);
        if (Vector3.Distance(flatPos, flatTar) < waypointSwitchRadius)
            currentIndex = (currentIndex + 1) % waypoints.Length;

        // ---- traffic light / stop check ----
        bool mustStopForJunction = false;
        SimpleTrafficLightController nearest = GetNearestController();
        if (nearest != null)
            mustStopForJunction = nearest.ShouldStopForCar(this.transform, currentSpeed, brakeAccel);

        // ---- front-back collision check ----
        bool mustStopForCar = HasFrontBackContact;

        // ---- combined decision ----
        bool mustStop = mustStopForJunction || mustStopForCar;

        float targetSpeed = mustStop ? 0f : speed;
        float rate = mustStop ? brakeAccel : accel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

        // ---- move ----
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
    }

    SimpleTrafficLightController GetNearestController()
    {
        SimpleTrafficLightController bestCtrl = null;
        float bestDist = float.PositiveInfinity;
        var list = SimpleTrafficLightController.All.Count > 0
            ? SimpleTrafficLightController.All
            : new System.Collections.Generic.List<SimpleTrafficLightController>(FindObjectsOfType<SimpleTrafficLightController>());

        foreach (var ctrl in list)
        {
            if (!ctrl) continue;
            if (!ctrl.IsCarAtThisJunction(transform)) continue; // only consider controllers whose stop spheres contain us

            float d = Vector3.Distance(transform.position, ctrl.transform.position);
            if (d < bestDist) { bestDist = d; bestCtrl = ctrl; }
        }
        return bestCtrl; // null if we’re not “at” any junction yet
    }

}
