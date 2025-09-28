using UnityEngine;

[DisallowMultipleComponent]
public class CarDriverHead : MonoBehaviour, IDamageable
{
    [Tooltip("Reference to the car's driver state on the car root.")]
    public CarDriverState car;

    [Tooltip("How many hits until the driver dies. Works with your gun's 'damage' value.")]
    public int health = 1;

    Collider col;

    void Reset()
    {
        // Try to find a non-trigger collider automatically
        col = GetComponent<Collider>();
        if (!col) col = gameObject.AddComponent<SphereCollider>(); // simple sphere if none
        col.isTrigger = false; // IMPORTANT: your gun raycasts ignore triggers
        // Give it a kinematic RB so it can get forces without falling
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Awake()
    {
        col = GetComponent<Collider>();
        if (!col) Debug.LogWarning("CarDriverHead: no Collider found. Add one and keep it NON-TRIGGER.", this);
        else col.isTrigger = false;
    }

    public void TakeDamage(int damage)
    {
        if (health <= 0) return;
        health -= Mathf.Max(1, damage);

        if (health <= 0)
        {
            // Notify car that the driver is dead
            if (car) car.OnDriverKilled();
            else Debug.LogWarning("CarDriverHead: No CarDriverState assigned.", this);
        }
    }
}
