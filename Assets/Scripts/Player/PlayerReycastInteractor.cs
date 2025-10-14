using UnityEngine;

public class PlayerRaycastInteractor : MonoBehaviour
{
    [Header("Raycasting")]
    public Camera cam;                 // assign your player camera (or leave null to auto-grab main)
    public float interactDistance = 3f;
    public LayerMask interactMask;     // set to layer(s) your door root/collider is on

    [Header("Input")]
    public KeyCode useKey = KeyCode.E;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (!Input.GetKeyDown(useKey)) return;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            // Look for DoubleDoor on the hit object or its parents
            var door = hit.collider.GetComponentInParent<DoubleDoor>();
            if (door != null)
            {
                door.Toggle();
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Camera c = cam != null ? cam : Camera.main;
        if (c == null) return;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(c.transform.position, c.transform.position + c.transform.forward * interactDistance);
    }
#endif
}