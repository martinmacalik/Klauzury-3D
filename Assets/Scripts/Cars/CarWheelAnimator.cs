using UnityEngine;

public class CarWheelAnimator : MonoBehaviour
{
    [System.Serializable]
    public enum Axis { X, Y, Z }

    [System.Serializable]
    public class Wheel
    {
        public Transform visual;      // mesh to rotate (the thing you see)
        public Transform reference;   // transform whose forward matches rolling direction (usually the wheel root that steers)
        public float radius = 0.35f;  // in meters
        public Axis spinAxis = Axis.X;// which local axis the mesh should spin around
        public bool invert;           // flip if it spins backwards
    }

    [SerializeField] Rigidbody carRB; // car rigidbody
    [SerializeField] Wheel[] wheels;

    void Reset()
    {
        carRB = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (!carRB || wheels == null) return;

        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (!w.visual || !w.reference || w.radius <= 0.001f) continue;

            // linear velocity at this wheel's position
            Vector3 v = carRB.GetPointVelocity(w.reference.position);

            // speed along the wheel's rolling direction (the ref's forward)
            float forwardSpeed = Vector3.Dot(v, w.reference.forward);

            // angular velocity (rad/s) -> degrees per frame
            float angVel = forwardSpeed / w.radius;
            float deg = angVel * Mathf.Rad2Deg * Time.deltaTime;
            if (w.invert) deg = -deg;

            // spin the mesh around its chosen local axis
            Vector3 axis =
                w.spinAxis == Axis.X ? Vector3.right :
                w.spinAxis == Axis.Y ? Vector3.up    :
                Vector3.forward;

            w.visual.Rotate(axis, deg, Space.Self);
        }
    }
}