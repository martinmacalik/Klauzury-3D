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
    [SerializeField] float stopSpeedThreshold = 0.25f; // m/s at which we consider it "stopped"
    [SerializeField] float maxBrakeTime = 2.5f;        // hard cap so it never holds forever
    
    [SerializeField] SimpleGun gun;   // (optional) assign in Inspector

    bool inCar;
    bool playerInTrigger;
    bool aiLockedOut;            // once player drives, AI never comes back
    Coroutine brakeCo;

    Rigidbody rb;                // for speed check

    void Reset()
    {
        enterTrigger = GetComponent<Collider>();
        carController = GetComponentInParent<WheelCarController>();
        aiDriver = GetComponentInParent<CarAIDriver>();
    }

    void Awake()
    {
        if (enterTrigger) enterTrigger.isTrigger = true;

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
        if (!playerRoot) return;
        if (other.transform == playerRoot.transform || other.CompareTag("Player"))
            playerInTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!playerRoot) return;
        if (other.transform == playerRoot.transform || other.CompareTag("Player"))
            playerInTrigger = false;
    }

    void Update()
    {
        if (!inCar && playerInTrigger && Input.GetKeyDown(enterKey))
            EnterCar();

        if (inCar && Input.GetKeyDown(exitKey))
            ExitCar();
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
            worldPos = transform.position + transform.right * exitRightMeters + transform.forward * exitForwardMeters;
            worldRot = Quaternion.LookRotation(new Vector3(transform.forward.x, 0f, transform.forward.z), Vector3.up);

            // drop to ground if needed
            Ray ray = new Ray(worldPos + Vector3.up * groundRaycast, Vector3.down);
            if (Physics.Raycast(ray, out var hit, groundRaycast * 2f, ~0, QueryTriggerInteraction.Ignore))
                worldPos = hit.point;
        }

        // show player again
        playerRoot.transform.SetPositionAndRotation(worldPos, worldRot);
        playerRoot.SetActive(true);
        HardLockWeapon();   

        // turn off car cam
        if (carCamera) carCamera.gameObject.SetActive(false);

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

        // release inputs after stopping or timeout
        carController.SetExternalInputs(0f, 0f);
        brakeCo = null;
    }
    
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
