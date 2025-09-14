using UnityEngine;

public class WheelAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform[] frontWheels;   // wheels that steer
    public Transform[] rearWheels;    // wheels that just spin

    [Header("Wheel Spin")]
    public float wheelRadius = 0.35f; // meters

    [Header("Steer Visuals")]
    public float maxSteerAngle = 30f;     // degrees

    [Tooltip("How fast the wheels turn into a steering angle (deg/sec).")]
    public float steerInSpeed = 360f;     // quick turn-in
    [Tooltip("How fast the wheels return toward center (deg/sec).")]
    public float steerOutSpeed = 540f;    // even quicker return
    [Tooltip("Snap-to-center when within this many degrees of 0.")]
    public float centerDeadzone = 0.5f;

    private CarAI carAI;
    private float wheelSpin = 0f;
    private float currentSteerAngle = 0f;

    void Start()
    {
        carAI = GetComponent<CarAI>();
    }

    void Update()
    {
        if (carAI == null) return;

        // --- Spin based on forward speed ---
        float distanceThisFrame = carAI.CurrentSpeed * Time.deltaTime;
        float spinDegrees = (distanceThisFrame / (2f * Mathf.PI * wheelRadius)) * 360f;
        wheelSpin += spinDegrees;

        // --- Target steer toward next waypoint ---
        float targetSteer = 0f;
        if (carAI.waypoints.Length > 0)
        {
            Transform target = carAI.waypoints[carAI.currentIndex]; // currentIndex should be public/HideInInspector
            Vector3 dir = (target.position - transform.position).normalized;
            Vector3 localDir = transform.InverseTransformDirection(dir);

            targetSteer = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            targetSteer = Mathf.Clamp(targetSteer, -maxSteerAngle, maxSteerAngle);
        }

        // --- Choose rate: faster when returning toward center ---
        // If the absolute target is smaller than current, we're coming back toward center.
        float chosenSpeed = (Mathf.Abs(targetSteer) < Mathf.Abs(currentSteerAngle))
            ? steerOutSpeed
            : steerInSpeed;

        // Move at a constant angular speed (no easing)
        currentSteerAngle = Mathf.MoveTowardsAngle(currentSteerAngle, targetSteer, chosenSpeed * Time.deltaTime);

        // Small snap to zero to avoid micro-jitter when nearly straight
        if (Mathf.Abs(currentSteerAngle) < centerDeadzone && Mathf.Abs(targetSteer) < centerDeadzone)
            currentSteerAngle = 0f;

        // --- Apply rotations ---
        // Rear: spin only
        foreach (Transform wheel in rearWheels)
            wheel.localRotation = Quaternion.Euler(wheelSpin, 0f, 0f);

        // Front: steering (y) * spin (x)
        foreach (Transform wheel in frontWheels)
        {
            Quaternion spin = Quaternion.Euler(wheelSpin, 0f, 0f);
            Quaternion steer = Quaternion.Euler(0f, currentSteerAngle, 0f);
            wheel.localRotation = steer * spin;
        }
    }
}
