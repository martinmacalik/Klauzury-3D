// CarDriverState.cs (additions marked // NEW)
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WheelCarController))]
public class CarDriverState : MonoBehaviour
{
    [Header("References")]
    public CarAIDriver ai;
    public WheelCarController car;

    [Header("Driver Body")]                      // NEW
    [Tooltip("Root GameObject of the visible NPC driver inside the car.")]
    public GameObject driverBodyRoot;            // NEW

    [Header("Stopping Behaviour")]
    public float stopBlendSeconds = 1.0f;
    [Range(0.1f, 1f)] public float brakeStrength = 0.75f;
    public float stopThreshold = 0.25f;

    [Header("Takeover")]
    public bool allowPlayerTakeover = true;

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
        if (ai && ai.enabled && car) car.SetControlMode(WheelCarController.ControlMode.External);
    }

    void Update()
    {
        if (!braking || !car || rb == null) return;
        car.SetControlMode(WheelCarController.ControlMode.External);

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedAbs = Mathf.Abs(forwardSpeed);

        float t = Mathf.Clamp01((Time.time - killTime) / Mathf.Max(0.001f, stopBlendSeconds));
        float commandedBrake = Mathf.Lerp(0f, -brakeStrength, t);
        float throttleCmd = (forwardSpeed > 0f) ? commandedBrake : 0f;

        car.SetExternalInputs(throttleCmd, 0f);

        if (speedAbs <= stopThreshold)
        {
            car.SetExternalInputs(0f, 0f);
            braking = false;
        }
    }

    public void OnDriverKilled()
    {
        if (!driverAlive) return;
        driverAlive = false;

        if (ai) ai.enabled = false;

        killTime = Time.time;
        braking  = true;

        if (car) car.SetControlMode(WheelCarController.ControlMode.External);

        HideDriverBody();                         // NEW
    }

    public void EnterAsPlayer()
    {
        if (!car) return;
        if (!allowPlayerTakeover) return;

        if (!driverAlive || (ai && !ai.enabled))
        {
            braking = false;
            car.SetExternalInputs(0f, 0f);
            car.SetControlMode(WheelCarController.ControlMode.Player);
        }
    }

    public void ReEnableAI()
    {
        driverAlive = true;
        if (ai) ai.enabled = true;
        if (car) car.SetControlMode(WheelCarController.ControlMode.External);
        ShowDriverBody();                         // NEW (optional)
    }

    // --- NEW: body visibility helpers ---
    public void HideDriverBody()
    {
        if (driverBodyRoot && driverBodyRoot.activeSelf)
            driverBodyRoot.SetActive(false);
    }

    public void ShowDriverBody()
    {
        if (driverBodyRoot && !driverBodyRoot.activeSelf)
            driverBodyRoot.SetActive(true);
    }
}
