using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class SensorNode : MonoBehaviour
{
    public enum SensorType { Front, Back }
    public SensorType type = SensorType.Front;

    [Tooltip("Owner car (root with CarAI). Set automatically on Awake if empty.")]
    public CarAI owner;

    void Awake()
    {
        // Make sure collider is trigger + rigidbody is kinematic
        var sc = GetComponent<SphereCollider>();
        sc.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Auto-find owner
        if (!owner) owner = GetComponentInParent<CarAI>();
    }

    void OnTriggerEnter(Collider other)
    {
        var otherNode = other.GetComponent<SensorNode>();
        if (!otherNode || otherNode.owner == owner) return;

        // Brake only when MY FRONT hits THEIR BACK
        if (type == SensorType.Front && otherNode.type == SensorType.Back)
        {
            owner.NotifyFrontBackEnter(otherNode.owner);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var otherNode = other.GetComponent<SensorNode>();
        if (!otherNode || otherNode.owner == owner) return;

        if (type == SensorType.Front && otherNode.type == SensorType.Back)
        {
            owner.NotifyFrontBackExit(otherNode.owner);
        }
    }
}

