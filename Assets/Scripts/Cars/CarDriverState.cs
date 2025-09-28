using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WheelCarController))]
public class CarDriverState : MonoBehaviour
{
    [Header("References")]
    [Tooltip("AI driver to disable when the driver dies.")]
    public CarAIDriver ai;
    [Tooltip("Wheel car controller that will be gently braked to a stop.")]
    public WheelCarController car;

    [Header("Stopping Behaviour")]
    [Tooltip("How quickly the car blends into braking after the driver is killed (seconds).")]
    public float stopBlendSeconds = 1.0f;
    [Tooltip("How hard to brake while coasting to a stop (External throttle in [-1..0]).")]
    [Range(0.1f, 1f)] public float brakeStrength = 0.75f;
    [Tooltip("Speed (m/s) at which we consider the car stopped.")]
    public float stopThreshold = 0.25f;

    [Header("Takeover")]
    [Tooltip("If true, player can take over after driver death using EnterAsPlayer().")]
    public bool allowPlayerTakeover = true;

    // runtime
    bool driverAlive = true;
    bool braking = false;
    float killTime;
    Rigidbody rb;

    void Awake()
    {
        if (!car) car = GetComponent<WheelCarController>();
        if (!ai)  ai  = GetComponent<CarAIDriver>();
        rb = car ? car.RB : GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        // Default: let AI own the inputs if present
        if (ai && ai.enabled && car) car.SetControlMode(WheelCarController.ControlMode.External);
    }

    void Update()
    {
        if (!braking || !car || rb == null) return;

        // Force External control while we are managing the stop
        car.SetControlMode(WheelCarController.ControlMode.External);

        // Forward speed (signed along the car’s forward)
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedAbs = Mathf.Abs(forwardSpeed);

        // Blend into brake over 'stopBlendSeconds' for a smooth reaction
        float t = Mathf.Clamp01((Time.time - killTime) / Mathf.Max(0.001f, stopBlendSeconds));
        float commandedBrake = Mathf.Lerp(0f, -brakeStrength, t);

        // If moving forward, apply brake; if rolling backward, just stop commanding more reverse
        float throttleCmd = (forwardSpeed > 0f) ? commandedBrake : 0f;

        // Steer straight while stopping
        car.SetExternalInputs(throttleCmd, 0f);

        // When slow enough, release inputs
        if (speedAbs <= stopThreshold)
        {
            car.SetExternalInputs(0f, 0f);
            braking = false; // we’re done stopping
        }
    }

    /// <summary>
    /// Called by CarDriverHead when health reaches zero.
    /// </summary>
    public void OnDriverKilled()
    {
        if (!driverAlive) return;
        driverAlive = false;

        // Disable AI so it stops issuing commands
        if (ai) ai.enabled = false;

        // Begin braking phase
        killTime = Time.time;
        braking  = true;

        // Ensure we're in external mode while we slow down
        if (car) car.SetControlMode(WheelCarController.ControlMode.External);
    }

    /// <summary>
    /// Call this from your "enter car" interaction script after the player gets in.
    /// Gives the player control if the driver is dead (or AI is off).
    /// </summary>
    public void EnterAsPlayer()
    {
        if (!car) return;
        if (!allowPlayerTakeover) return;

        // Only hand over when the driver's gone (or AI is disabled for any reason)
        if (!driverAlive || (ai && !ai.enabled))
        {
            braking = false;                       // stop our braking loop
            car.SetExternalInputs(0f, 0f);         // neutral
            car.SetControlMode(WheelCarController.ControlMode.Player);
        }
    }

    /// <summary>
    /// Optionally call this to put the car back under AI control (e.g., respawn driver).
    /// </summary>
    public void ReEnableAI()
    {
        driverAlive = true;
        if (ai) ai.enabled = true;
        if (car) car.SetControlMode(WheelCarController.ControlMode.External);
    }
}
