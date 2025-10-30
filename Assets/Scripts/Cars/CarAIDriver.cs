using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WheelCarController))]
public class CarAIDriver : MonoBehaviour
{
    // ------- tunables -------
    public Transform[] waypoints;
    public float waypointSwitchRadius = 1f;
    public float lookAheadMeters = 6f;

    public float cruiseSpeed = 10f;       // m/s
    [Range(0.1f, 1f)] public float maxSteerCmd = 0.85f;
    public float steerGain = 2f;          // higher = snappier turn-in
    public float speedPGain = 0.25f;      // maps speed error to throttle/brake

    // short-range anti-collision bubble
    public float bubbleRadius = 1f;
    public float bubbleForward = 2f;
    public float bubbleHeight = 0.5f;
    public float brakeEarlyMeters = 1f;
    public LayerMask carLayer = ~0;

    // misc
    public float stopBuffer = 1f;         // extra margin on TL stop

    // runtime
    [HideInInspector] public int currentIndex = 0;

    WheelCarController car;
    Rigidbody rb;
    AICarSensor sensor;
    float currentSpeed;
    float desiredSpeed;

    void Awake()
    {
        car = GetComponent<WheelCarController>();
        rb  = car ? car.RB : GetComponent<Rigidbody>();

        // Initialize waypoints if null to prevent Unity serialization errors
        if (waypoints == null)
            waypoints = new Transform[0];

        // Only create sensor if we have waypoints (AI will actually drive)
        if (waypoints.Length > 0)
        {
            // make a tiny trigger sphere in front of the car
            var sensorGO = new GameObject("AI_Sensor");
            sensorGO.transform.SetParent(transform, false);
            sensorGO.transform.localPosition = new Vector3(0f, bubbleHeight, bubbleForward + brakeEarlyMeters);
            sensorGO.layer = gameObject.layer;

            var sc = sensorGO.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = bubbleRadius;

            var srb = sensorGO.AddComponent<Rigidbody>();
            srb.isKinematic = true; srb.useGravity = false;

            sensor = sensorGO.AddComponent<AICarSensor>();
            sensor.owner = this;
            sensor.carLayerMask = carLayer;
        }
    }

    void OnEnable()
    {
        if (car) car.SetControlMode(WheelCarController.ControlMode.External);
        
        // If no waypoints, park the car immediately
        if (waypoints == null || waypoints.Length == 0)
        {
            if (rb)
                ParkCar();
        }
    }

    void Update()
    {
        // nothing to do? -> idle inputs and keep parked
        if (car == null || rb == null)
        {
            if (car) car.SetExternalInputs(0f, 0f);
            if (rb) ParkCar();
            return;
        }

        // Ensure waypoints array is not null (safety)
        if (waypoints == null)
            waypoints = new Transform[0];

        // If no waypoints assigned, park and idle
        if (waypoints.Length == 0)
        {
            if (car) car.SetExternalInputs(0f, 0f);
            if (rb) ParkCar();
            return;
        }

        // Clamp currentIndex into valid range
        if (currentIndex < 0) currentIndex = 0;
        if (currentIndex >= waypoints.Length) currentIndex = 0;

        // Find the next non-null waypoint (search up to waypoints.Length entries)
        Transform target = null;
        int foundIndex = -1;
        for (int i = 0; i < waypoints.Length; i++)
        {
            int idx = (currentIndex + i) % waypoints.Length;
            if (waypoints[idx] != null)
            {
                target = waypoints[idx];
                foundIndex = idx;
                break;
            }
        }

        // If no valid waypoint found, clear waypoints and park
        if (target == null)
        {
            // replace with empty to avoid repeated checks elsewhere
            waypoints = new Transform[0];
            if (car) car.SetExternalInputs(0f, 0f);
            if (rb) ParkCar();
            return;
        }

        // make sure currentIndex points to the valid target
        currentIndex = foundIndex;

        // keep sensor in sync if tweaked in inspector at runtime
        if (sensor) sensor.SyncSphere(bubbleRadius, bubbleForward + brakeEarlyMeters, bubbleHeight);

        // speed along forward axis (signed)
        currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // waypoint switching (flat distance)
        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTar = new Vector3(target.position.x, 0f, target.position.z);

        if (Vector3.Distance(flatPos, flatTar) < waypointSwitchRadius)
        {
            // advance to next valid waypoint
            int attempts = 0;
            int next = currentIndex;
            do
            {
                next = (next + 1) % waypoints.Length;
                attempts++;
                if (waypoints[next] != null)
                {
                    currentIndex = next;
                    target = waypoints[currentIndex];
                    flatTar = new Vector3(target.position.x, 0f, target.position.z);
                    break;
                }
            } while (attempts < waypoints.Length);

            // if we couldn't find a valid next target, park and bail out
            if (target == null || waypoints.Length == 0 || attempts >= waypoints.Length && (waypoints[currentIndex] == null))
            {
                waypoints = new Transform[0];
                if (car) car.SetExternalInputs(0f, 0f);
                if (rb) ParkCar();
                return;
            }
        }

        // simple look-ahead toward the current waypoint
        Vector3 toTar = (flatTar - flatPos);
        Vector3 dir = toTar.sqrMagnitude > 0.0001f ? toTar.normalized : transform.forward;
        Vector3 aimPoint = flatPos + dir * Mathf.Clamp(lookAheadMeters, 0.5f, 100f);

        // steering from local aim (atan2 side/forward)
        Vector3 localAim = transform.InverseTransformPoint(new Vector3(aimPoint.x, transform.position.y, aimPoint.z));
        float steerRaw = Mathf.Atan2(localAim.x, Mathf.Max(0.01f, localAim.z));
        float steerCmd = Mathf.Clamp(steerRaw * steerGain, -maxSteerCmd, maxSteerCmd);

        // stop/go: traffic light + car bubble
        bool stopTL = false;
        var tl = GetNearestController();
        if (tl != null)
        {
            float brakeAccel = GetApproxBrakeAccel();
            stopTL = tl.ShouldStopForCar(this.transform, Mathf.Abs(currentSpeed), brakeAccel);
        }
        bool stopCar = sensor != null && sensor.HasCarAheadThisFrame(transform);
        bool mustStop = stopTL || stopCar;

        // desired speed
        desiredSpeed = mustStop ? 0f : cruiseSpeed;

        // extra conservative clamp if near a junction
        if (!mustStop && tl != null && tl.IsCarAtThisJunction(transform))
        {
            float a = Mathf.Max(0.01f, GetApproxBrakeAccel());
            float stoppingDistance = (currentSpeed * currentSpeed) / (2f * a) + stopBuffer;
            if (toTar.magnitude < stoppingDistance && tl.ShouldStopForCar(this.transform, Mathf.Abs(currentSpeed), a))
                desiredSpeed = 0f;
        }

        // speed -> throttle (P controller). Negative = brake.
        float throttle = Mathf.Clamp(speedPGain * (desiredSpeed - currentSpeed), -1f, 1f);
        if (desiredSpeed >= 0f && throttle < 0f) throttle = Mathf.Max(throttle, -0.7f); // don't reverse

        // drive
        car.SetExternalInputs(throttle, steerCmd);
    }

    void ParkCar()
    {
        if (!rb) return;  // safety check
        
        // Ensure the car is not moving
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    float GetApproxBrakeAccel()
    {
        // If your controller exposes real decel, use that; this is a safe default.
        return 10f;
    }

    SimpleTrafficLightController GetNearestController()
    {
        SimpleTrafficLightController nearest = null;
        float best = float.PositiveInfinity;

        if (SimpleTrafficLightController.All.Count > 0)
        {
            foreach (var c in SimpleTrafficLightController.All)
            {
                if (!c) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < best) { best = d; nearest = c; }
            }
        }
        else
        {
            foreach (var c in FindObjectsOfType<SimpleTrafficLightController>())
            {
                if (!c) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < best) { best = d; nearest = c; }
            }
        }
        return nearest;
    }
}

// small trigger that remembers cars inside and checks if any are actually ahead
public class AICarSensor : MonoBehaviour
{
    [HideInInspector] public CarAIDriver owner;
    [HideInInspector] public LayerMask carLayerMask;

    readonly HashSet<Transform> inside = new HashSet<Transform>();
    SphereCollider sphere;

    void Awake() { sphere = GetComponent<SphereCollider>(); }

    public void SyncSphere(float radius, float forward, float height)
    {
        if (!sphere) return;
        sphere.radius = radius;
        transform.localPosition = new Vector3(0f, height, forward);
    }

    public bool HasCarAheadThisFrame(Transform self)
    {
        inside.RemoveWhere(t => t == null);

        foreach (var t in inside)
        {
            if (!t) continue;
            if (t == self || t.IsChildOf(self)) continue; // ignore self

            // count anything with AI or controller as a "car"
            if (!t.GetComponentInParent<CarAIDriver>() && !t.GetComponentInParent<WheelCarController>()) continue;

            // only if it's in front (flat)
            Vector3 toOther = t.position - self.position; toOther.y = 0f;
            if (toOther.sqrMagnitude < 0.0001f) continue;
            if (Vector3.Dot(self.forward, toOther.normalized) <= 0f) continue;

            return true;
        }
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & carLayerMask.value) == 0) return;
        inside.Add(other.transform);
    }

    void OnTriggerExit(Collider other) => inside.Remove(other.transform);

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & carLayerMask.value) == 0) return;
        inside.Add(other.transform);
    }
}
