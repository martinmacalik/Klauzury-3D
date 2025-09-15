using UnityEngine;
using System.Linq;

public class PlayerVehicleInteractor : MonoBehaviour
{
    [Header("Player Bits")]
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private GameObject modelRoot;
    [SerializeField] private MonoBehaviour[] movementScriptsToDisable;
    [SerializeField] private CharacterController characterController;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactRadius = 3.0f;     // how close to detect a car
    [SerializeField] private LayerMask vehicleMask = ~0;       // set to a specific layer if you have one
    [SerializeField] private float exitOffset = 1.5f;

    [Header("Optional UI")]
    [SerializeField] private GameObject enterPrompt;

    private VehicleSeat currentVehicle;
    private bool inVehicle;

    private void Reset()
    {
        if (!firstPersonCamera) firstPersonCamera = GetComponentInChildren<Camera>();
        if (!characterController) characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        VehicleSeat nearest = null;
        float nearestDist = float.MaxValue;

        if (!inVehicle)
        {
            // Look for any VehicleSeat within a sphere around the player
            var hits = Physics.OverlapSphere(transform.position, interactRadius, vehicleMask, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                if (h.TryGetComponent<VehicleSeat>(out var seat))
                {
                    float d = Vector3.SqrMagnitude(seat.transform.position - transform.position);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = seat;
                    }
                }
                else
                {
                    // If the collider is on the car root and VehicleSeat is on the parent
                    var seatInParent = h.GetComponentInParent<VehicleSeat>();
                    if (seatInParent)
                    {
                        float d = Vector3.SqrMagnitude(seatInParent.transform.position - transform.position);
                        if (d < nearestDist)
                        {
                            nearestDist = d;
                            nearest = seatInParent;
                        }
                    }
                }
            }
        }

        if (enterPrompt) enterPrompt.SetActive(!inVehicle && nearest != null);

        if (Input.GetKeyDown(interactKey))
        {
            if (!inVehicle && nearest != null)
            {
                EnterVehicle(nearest);
            }
            else if (inVehicle && currentVehicle != null)
            {
                ExitVehicle();
            }
        }
    }

    private void EnterVehicle(VehicleSeat vehicle)
    {
        if (inVehicle) return;

        // 1) Freeze player
        SetPlayerActive(false);

        // 2) Snap to seat
        Transform seat = vehicle.SeatTransform;
        if (seat) transform.SetPositionAndRotation(seat.position, seat.rotation);

        // 3) Enable car camera/control, THEN disable FPS camera
        vehicle.SetOccupied(true);
        if (firstPersonCamera) firstPersonCamera.enabled = false;

        currentVehicle = vehicle;
        inVehicle = true;
    }

    private void ExitVehicle()
    {
        if (!inVehicle || currentVehicle == null) return;

        // 1) Pick exit spot
        Vector3 exitPos = currentVehicle.transform.position + currentVehicle.transform.right * exitOffset;
        exitPos.y = transform.position.y;
        transform.position = exitPos;

        // 2) Re-enable FPS camera/control FIRST so there’s always a camera
        if (firstPersonCamera) firstPersonCamera.enabled = true;
        currentVehicle.SetOccupied(false);

        // 3) Unfreeze player
        SetPlayerActive(true);

        currentVehicle = null;
        inVehicle = false;
    }


    private void SetPlayerActive(bool active)
    {
        if (movementScriptsToDisable != null)
        {
            foreach (var mb in movementScriptsToDisable)
                if (mb) mb.enabled = active;
        }

        if (characterController) characterController.enabled = active;
        if (modelRoot) modelRoot.SetActive(active);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
#endif
}
