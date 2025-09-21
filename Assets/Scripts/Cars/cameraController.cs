using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // assign your car root here

    [Header("Follow Settings")]
    [Tooltip("How quickly the camera reaches the target pose (higher = snappier).")]
    public float followSpeed = 8f;        // position smoothing speed
    public float rotateSpeed = 8f;        // rotation smoothing speed

    // cached local (relative) offset pose from target
    private Vector3 localPosOffset;       // target-local position of the camera
    private Quaternion localRotOffset;    // target-local rotation of the camera

    void Start()
    {
        if (!target) { Debug.LogError("[CameraController] No target assigned."); enabled = false; return; }
        localPosOffset = target.InverseTransformPoint(transform.position);
        localRotOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
    }

    void LateUpdate() // <- was FixedUpdate
    {
        Vector3 desiredPos = target.TransformPoint(localPosOffset);
        Quaternion desiredRot = target.rotation * localRotOffset;

        float posT = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        float rotT = 1f - Mathf.Exp(-rotateSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPos, posT);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotT);
    }
    
    public void SetTarget(Transform newTarget, bool snap = true)
    {
        if (!newTarget) return;

        // bind + (re)compute offsets from the camera's CURRENT pose
        target = newTarget;
        localPosOffset = target.InverseTransformPoint(transform.position);
        localRotOffset = Quaternion.Inverse(target.rotation) * transform.rotation;

        if (snap)
        {
            transform.position = target.TransformPoint(localPosOffset);
            transform.rotation = target.rotation * localRotOffset;
        }

        // in case Start() disabled the component when no target was set
        enabled = true;
    }

}