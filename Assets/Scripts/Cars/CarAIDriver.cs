// CarAIDriver.cs  (NEW — use this instead of your old CarAI mover)
using UnityEngine;

[RequireComponent(typeof(WheelCarController))]
public class CarAIDriver : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;
    [Tooltip("How close to a waypoint before switching to the next.")]
    public float waypointSwitchRadius = 2.0f;
    public int currentIndex = 0;

    [Header("Driving")]
    public float cruiseSpeed = 12f;       // m/s
    public float maxSteerAtLowSpeed = 1f; // input magnitude at low speeds
    public float steerResponsiveness = 3f;
    public float throttleResponsiveness = 2f;

    [Header("Ground Align (optional)")]
    public LayerMask groundMask;
    public float alignRayHeight = 2f;
    public float alignRayDown = 4f;
    public float normalLerp = 8f;

    private WheelCarController car;
    private Rigidbody rb;

    void Awake()
    {
        car = GetComponent<WheelCarController>();
        rb  = car.RB;
        car.SetControlMode(WheelCarController.ControlMode.External);
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];
        Vector3 toTarget = (target.position - transform.position);
        toTarget.y = 0f;

        // Advance waypoint
        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTar = new Vector3(target.position.x, 0, target.position.z);
        if (Vector3.Distance(flatPos, flatTar) < waypointSwitchRadius)
            currentIndex = (currentIndex + 1) % waypoints.Length;

        // Desired steer: signed angle to target
        float steer = 0f;
        if (toTarget.sqrMagnitude > 0.01f)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f;
            float signedAngle = Vector3.SignedAngle(fwd, toTarget.normalized, Vector3.up);
            // Map degrees to [-1,1]
            steer = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
            // Reduce max steer with speed so it doesn’t oscillate
            float speed01 = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(1f, cruiseSpeed));
            float steerLimit = Mathf.Lerp(maxSteerAtLowSpeed, 0.25f, speed01);
            steer = Mathf.Clamp(steer, -steerLimit, steerLimit);
        }

        // Desired throttle: aim for cruise speed
        float speedError = cruiseSpeed - rb.linearVelocity.magnitude;
        float throttle = Mathf.Clamp(speedError * 0.15f, -1f, 1f);

        // Responsiveness smoothing
        float currentThrottle = Mathf.Lerp(0f, throttle, 1f - Mathf.Exp(-throttleResponsiveness * Time.fixedDeltaTime));
        float currentSteer    = Mathf.Lerp(0f, steer,    1f - Mathf.Exp(-steerResponsiveness    * Time.fixedDeltaTime));

        car.SetExternalInputs(currentThrottle, currentSteer);

        // Optional: align “up” with ground normal for better stability
        if (groundMask.value != 0)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * alignRayHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, alignRayDown + alignRayHeight, groundMask))
            {
                // current and desired orientations
                Quaternion toGround = Quaternion.FromToRotation(transform.up, hit.normal);

                // convert small rotation to axis-angle
                toGround.ToAngleAxis(out float angleDeg, out Vector3 axis);
                if (angleDeg > 180f) angleDeg -= 360f;

                // apply gentle corrective torque proportional to angle
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 correctiveTorque = axis.normalized * angleRad * normalLerp; // normalLerp ~ 8 is okay
                rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
            }
        }
    }

    // Allow other systems to enable/disable AI driving
    public void EnableAI(bool enabled)
    {
        enabled = enabled && (waypoints != null && waypoints.Length > 0);
        enabledSelf = enabled;
        car.SetControlMode(enabled ? WheelCarController.ControlMode.External
                                   : WheelCarController.ControlMode.Player);
    }

    bool enabledSelf = true;
    void OnEnable()  { car.SetControlMode(WheelCarController.ControlMode.External); }
    void OnDisable() { car.SetControlMode(WheelCarController.ControlMode.Player);   }
}
