// WheelCarController.cs  (replace your class with this version)
using UnityEngine;

public class WheelCarController : MonoBehaviour
{
    public enum ControlMode { Player, External }   // NEW

    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask driveable;
    [SerializeField] private Transform accelerationPoint;

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness = 30000f;
    [SerializeField] private float damperStiffness = 3500f;
    [SerializeField] private float restLength = 0.35f;
    [SerializeField] private float springTravel = 0.2f;
    [SerializeField] private float wheelRadius = 0.33f;

    private int[] wheelsIsGrounded = new int[4];
    private bool isGrounded = false;

    [Header("Input")]
    [SerializeField] private ControlMode controlMode = ControlMode.Player; // NEW
    private float moveInput = 0f;
    private float steerInput = 0f;

    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve turningCurve =
        AnimationCurve.EaseInOut(0, 0.3f, 1, 1f);
    [SerializeField] private float dragCoefficient = 1f;

    private Vector3 currentCarLocalVelocity = Vector3.zero;
    private float carVelocityRatio = 0f;

    public void SetControlMode(ControlMode mode) => controlMode = mode;           // NEW
    public void SetExternalInputs(float throttle, float steer)                    // NEW
    {
        if (controlMode != ControlMode.External) return;
        moveInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
    }

    public Rigidbody RB => carRB;
    
    //WheelVisualManager script line
    public float Steer01 => steerInput; // -1..1 current steering command (player or AI)


    private void Start()
    {
        if (!carRB) carRB = GetComponent<Rigidbody>();
        carRB.interpolation = RigidbodyInterpolation.Interpolate;
        carRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        if (controlMode == ControlMode.Player)
        {
            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
        }
    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
    }

    // Movement
    private void Movement()
    {
        if (!isGrounded) return;

        // forward component of current velocity (m/s)
        float forwardSpeed = Vector3.Dot(carRB.linearVelocity, transform.forward);

        // Allow acceleration only if we’re under the speed cap
        if (moveInput > 0f && forwardSpeed < maxSpeed)
        {
            carRB.AddForceAtPosition(
                acceleration * moveInput * transform.forward,
                accelerationPoint.position,
                ForceMode.Acceleration
            );
        }

        // Braking / reverse
        if (moveInput < 0f)
        {
            carRB.AddForceAtPosition(
                deceleration * (-moveInput) * -transform.forward,
                accelerationPoint.position,
                ForceMode.Acceleration
            );
        }

        // Steering torque
        carRB.AddRelativeTorque(
            steerStrength * steerInput *
            turningCurve.Evaluate(Mathf.Abs(carVelocityRatio)) *
            Mathf.Sign(carVelocityRatio) * carRB.transform.up,
            ForceMode.Acceleration
        );

        // Sideways drag to stabilize
        float currentSidewaysSpeed = currentCarLocalVelocity.x;
        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;
        Vector3 dragForce = transform.right * dragMagnitude;
        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }



    // Car Status Check
    private void GroundCheck()
    {
        int tempGroundedWheels = 0;
        for (int i = 0; i < wheelsIsGrounded.Length; i++) tempGroundedWheels += wheelsIsGrounded[i];
        isGrounded = tempGroundedWheels > 1;
    }

    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        carVelocityRatio = Mathf.Clamp(currentCarLocalVelocity.z / Mathf.Max(1f, maxSpeed), -1f, 1f);
    }

    // Car suspension
    private void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            if (Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLength + wheelRadius, driveable))
            {
                wheelsIsGrounded[i] = 1;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = Mathf.Clamp01((restLength - currentSpringLength) / Mathf.Max(0.0001f, springTravel));

                // Damper
                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float dampForce = damperStiffness * springVelocity;

                float springForce = springStiffness * springCompression;
                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);

                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsIsGrounded[i] = 0;
                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLength) * -rayPoints[i].up, Color.green);
            }
        }
    }
}
