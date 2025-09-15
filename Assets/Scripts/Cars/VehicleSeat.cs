using UnityEngine;

public class VehicleSeat : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform seatTransform;
    [SerializeField] private Camera carCamera;                 // 3rd person camera, disabled by default
    [SerializeField] private MonoBehaviour vehicleController;  // your driving script (optional)

    public Transform SeatTransform => seatTransform;

    private bool occupied;

    private void Reset()
    {
        // Try to find a camera on this object or its children
        if (!carCamera) carCamera = GetComponentInChildren<Camera>(true);
    }

    private void Awake()
    {
        // Ensure initial state: no control, car cam off
        if (carCamera) carCamera.enabled = false;
        if (vehicleController) vehicleController.enabled = false;
    }

    public void SetOccupied(bool isOccupied)
    {
        // Switch camera first to avoid a frame with no camera
        if (isOccupied)
        {
            if (carCamera) carCamera.enabled = true;
            if (vehicleController) vehicleController.enabled = true;
        }
        else
        {
            if (vehicleController) vehicleController.enabled = false;
            if (carCamera) carCamera.enabled = false;
        }
    }
}