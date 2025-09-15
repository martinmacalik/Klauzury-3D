using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [SerializeField] private float accel = 15f;
    [SerializeField] private float steer = 60f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0); // a bit more stable
    }

    private void FixedUpdate()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");

        // Basic forward force
        Vector3 force = transform.forward * (v * accel);
        rb.AddForce(force, ForceMode.Acceleration);

        // Basic steering
        Quaternion turn = Quaternion.Euler(0f, h * steer * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }
}