using UnityEngine;

[DisallowMultipleComponent]
public class WheelVisuals : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private WheelCarController controller;

    [Header("Wheel Meshes")]
    [SerializeField] private Transform[] frontWheels;
    [SerializeField] private Transform[] rearWheels;

    [Header("Visual Settings")]
    [SerializeField] private float defaultRadius = 0.33f;   // one radius for all wheels
    [SerializeField] private float maxSteerAngle = 30f;     // visual steer degrees
    [SerializeField] private float steerSmoothing = 12f;    // higher = snappier

    [Header("Axes (change only if your meshes were imported differently)")]
    [SerializeField] private Axis spinAxis  = Axis.X;       // wheel roll axis (usually local X)
    [SerializeField] private Axis steerAxis = Axis.Y;       // steering yaw axis (usually local Y)
    [SerializeField] private bool invertSpin = false;       // flip if they roll backwards
    [SerializeField] private bool invertSteer = false;      // flip if left/right looks wrong

    // runtime
    private float steerDegSmoothed;
    private float[] frontRoll;
    private float[] rearRoll;
    private Quaternion[] frontBase;
    private Quaternion[] rearBase;

    void Reset()
    {
        if (!carRB) carRB = GetComponentInParent<Rigidbody>();
        if (!controller) controller = GetComponentInParent<WheelCarController>();
    }

    void Awake() { CacheBases(); }
    void OnValidate() { CacheBases(); }

    void CacheBases()
    {
        if (frontWheels != null)
        {
            if (frontBase == null || frontBase.Length != frontWheels.Length)
            {
                frontBase = new Quaternion[frontWheels.Length];
                frontRoll = new float[frontWheels.Length];
            }
            for (int i = 0; i < frontWheels.Length; i++)
                frontBase[i] = frontWheels[i] ? frontWheels[i].localRotation : Quaternion.identity;
        }

        if (rearWheels != null)
        {
            if (rearBase == null || rearBase.Length != rearWheels.Length)
            {
                rearBase = new Quaternion[rearWheels.Length];
                rearRoll = new float[rearWheels.Length];
            }
            for (int i = 0; i < rearWheels.Length; i++)
                rearBase[i] = rearWheels[i] ? rearWheels[i].localRotation : Quaternion.identity;
        }
    }

    void Update()
    {
        if (!carRB) return;

        // signed forward speed in m/s
        float speed = Vector3.Dot(carRB.linearVelocity, transform.forward);

        // live steering input from your controller (-1..1). 0 if not assigned.
        float steer01 = controller ? controller.Steer01 : 0f;

        // smooth the visual steering
        float targetSteer = maxSteerAngle * (invertSteer ? -steer01 : steer01);
        float s = 1f - Mathf.Exp(-Mathf.Max(0f, steerSmoothing) * Time.deltaTime);
        steerDegSmoothed = Mathf.Lerp(steerDegSmoothed, targetSteer, s);

        // precompute axes
        Vector3 spinVec  = AxisToVector(spinAxis);
        Vector3 steerVec = AxisToVector(steerAxis);

        // spin amount (deg this frame) from linear speed
        float angVelDegPerSec = (speed / (2f * Mathf.PI * Mathf.Max(0.01f, defaultRadius))) * 360f;
        float deltaSpin = angVelDegPerSec * Time.deltaTime * (invertSpin ? -1f : 1f);

        // FRONT: steer + spin
        for (int i = 0; i < (frontWheels?.Length ?? 0); i++)
        {
            var w = frontWheels[i];
            if (!w) continue;

            frontRoll[i] = Mathf.Repeat(frontRoll[i] + deltaSpin, 360f);

            Quaternion steerQ = Quaternion.AngleAxis(steerDegSmoothed, steerVec);
            Quaternion spinQ  = Quaternion.AngleAxis(frontRoll[i], spinVec);

            // order: base * steer * spin
            w.localRotation = frontBase[i] * steerQ * spinQ;
        }

        // REAR: spin only
        for (int i = 0; i < (rearWheels?.Length ?? 0); i++)
        {
            var w = rearWheels[i];
            if (!w) continue;

            rearRoll[i] = Mathf.Repeat(rearRoll[i] + deltaSpin, 360f);
            Quaternion spinQ = Quaternion.AngleAxis(rearRoll[i], spinVec);

            w.localRotation = rearBase[i] * spinQ;
        }
    }

    static Vector3 AxisToVector(Axis a)
    {
        switch (a)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            default:     return Vector3.forward;
        }
    }
}
