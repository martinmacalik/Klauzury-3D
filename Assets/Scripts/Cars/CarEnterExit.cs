using System.Collections;
using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    // core refs
    [SerializeField] WheelCarController carController;
    [SerializeField] CarAIDriver aiDriver;          // optional; will stay off after first player drive
    [SerializeField] GameObject playerRoot;         // has PlayerMovement + its own camera
    [SerializeField] Collider enterTrigger;         // trigger around the car (isTrigger = true)

    // car-only camera (separate from the player's)
    [SerializeField] Camera carCamera;              // disabled by default; enabled while driving
    [SerializeField] Transform carCamAnchor;        // snap pose for car camera (optional)

    // reference to state to hide/show NPC driver body
    [SerializeField] CarDriverState driverState;   // NEW

    // exit placement
    [SerializeField] Transform exitAnchor;          // optional exact exit spot
    [SerializeField] float exitRightMeters = 1.5f;  // fallback offsets if no anchor
    [SerializeField] float exitForwardMeters = 0.5f;
    [SerializeField] float groundRaycast = 2f;

    // keys
    [SerializeField] KeyCode enterKey = KeyCode.E;
    [SerializeField] KeyCode exitKey  = KeyCode.F;

    // post-exit braking (no Rigidbody drag monkeying)
    [SerializeField] float postExitBrake = 0.6f;      // 0..1 (negative throttle internally)
    [SerializeField] float stopSpeedThreshold = 0.25f;
    [SerializeField] float maxBrakeTime = 1.25f;

    // gating
    [SerializeField] float maxEnterSpeed = 1.5f;   // must be almost stopped to enter
    [SerializeField] float maxExitSpeed  = 1.0f;   // must be basically stopped to exit

    // internals
    bool inCar = false;
    bool aiLockedOut = false;   // once player drives, AI won't reenable on exit
    Coroutine brakeCo;
    SimpleGun gun;
    Rigidbody rb;                // for speed check

    void Reset()
    {
        enterTrigger = GetComponent<Collider>();
        carController = GetComponentInParent<WheelCarController>();
        aiDriver = GetComponentInParent<CarAIDriver>();
        driverState = GetComponentInParent<CarDriverState>();   // NEW
    }

    void Awake()
    {
        if (enterTrigger) enterTrigger.isTrigger = true;

        
        if (!driverState) driverState = GetComponentInParent<CarDriverState>(); // NEW

        // get a Rigidbody to read speed (prefer on same root as controller)
        rb = GetComponentInParent<Rigidbody>();
        if (!rb && carController != null)
        {
            // try common pattern: public RB getter on controller
            var prop = carController.GetType().GetProperty("RB");
            if (prop != null) rb = prop.GetValue(carController) as Rigidbody;
        }

        if (carCamera) carCamera.gameObject.SetActive(false);
        
        if (!gun && playerRoot)
            gun = playerRoot.GetComponentInChildren<SimpleGun>(true);
    }

    void OnTriggerEnter(Collider other)
    {
        // optional: could check player tag here for UI prompts, etc.
    }

    void Update()
    {
        if (!playerRoot || !carController) return;

        // basic key handling; you might swap to your input system
        if (!inCar)
        {
            // only enter if we're basically stopped
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            if (speed <= maxEnterSpeed && Input.GetKeyDown(enterKey))
            {
                EnterCar();
            }
        }
        else
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            if (speed <= maxExitSpeed && Input.GetKeyDown(exitKey))
            {
                ExitCar();
            }
        }
    }

    void EnterCar()
    {
        if (!playerRoot || !carController) return;

        // lock out AI forever after the first player drive
        aiLockedOut = true;
        if (aiDriver) aiDriver.enabled = false;

        // give controls to player
        carController.SetControlMode(WheelCarController.ControlMode.Player);
        carController.SetExternalInputs(0f, 0f); // clear any lingering external input

        // Hide the NPC body immediately when we take the seat
        if (driverState) driverState.HideDriverBody();  // NEW


        // snap & enable car camera
        if (carCamera)
        {
            if (carCamAnchor)
                carCamera.transform.SetPositionAndRotation(carCamAnchor.position, carCamAnchor.rotation);

            var cc = carCamera.GetComponent<CameraController>();
            if (cc) cc.SetTarget(carController.transform, snap: true);

            carCamera.gameObject.SetActive(true);
        }

        // hide player (disables their FPS cam + movement)
        HardLockWeapon();   
        playerRoot.SetActive(false);
        inCar = true;

        // stop any post-exit brake from previous cycle
        if (brakeCo != null) { StopCoroutine(brakeCo); brakeCo = null; }
    }

    void ExitCar()
    {
        if (!playerRoot || !carController) return;

        // choose exit pose
        Vector3 worldPos;
        Quaternion worldRot;

        if (exitAnchor)
        {
            worldPos = exitAnchor.position;
            worldRot = exitAnchor.rotation;
        }
        else
        {
            // default: a little to the right of car, slightly forward, snapped to ground
            var basePos = carController.transform.position 
                          + carController.transform.right * exitRightMeters
                          + carController.transform.forward * exitForwardMeters;

            // ground snap via raycast
            if (Physics.Raycast(new Ray(basePos + Vector3.up * groundRaycast, Vector3.down), out var hit, groundRaycast * 2f, ~0, QueryTriggerInteraction.Ignore))
                worldPos = hit.point;
            else
                worldPos = basePos;

            worldRot = Quaternion.LookRotation(carController.transform.forward, Vector3.up);
        }

        // disable car camera
        if (carCamera) carCamera.gameObject.SetActive(false);

        // unhide player and place them
        playerRoot.transform.SetPositionAndRotation(worldPos, worldRot);
        playerRoot.SetActive(true);
        WeaponHotkeys.GunIsReady = true;

        // do NOT re-enable AI if we've ever driven this car
        if (aiDriver && !aiLockedOut)
        {
            aiDriver.enabled = true;
            carController.SetControlMode(WheelCarController.ControlMode.External);
            carController.SetExternalInputs(0f, 0f);
        }
        else
        {
            // leave car in External mode with our controlled coasting brake
            carController.SetControlMode(WheelCarController.ControlMode.External);

            // start a brief braking phase so it rolls to a stop
            if (brakeCo != null) StopCoroutine(brakeCo);
            brakeCo = StartCoroutine(PostExitBrakeCoast());
        }

        inCar = false;
    }

    IEnumerator PostExitBrakeCoast()
    {
        float t = 0f;
        // apply a gentle "negative throttle" as brake, no steering
        while (t < maxBrakeTime)
        {
            float speed = rb ? rb.linearVelocity.magnitude : Mathf.Infinity;
            if (speed <= stopSpeedThreshold) break;

            carController.SetExternalInputs(-Mathf.Clamp01(postExitBrake), 0f);

            t += Time.deltaTime;
            yield return null;
        }

        // release control and neutral input
        carController.SetExternalInputs(0f, 0f);
    }

    // --- helpers ---

    void HardLockWeapon()
    {
        // Global lock – SimpleGun already respects this gate.
        WeaponHotkeys.GunIsReady = false;

        // Optional: snap animator out of ADS immediately if you want no flicker.
        if (gun && gun.TryGetComponent<Animator>(out var a))
        {
            a.ResetTrigger("Fire");
            a.SetBool("IsADS", false);
            a.SetBool("IsReady", false);
        }
    }

}
