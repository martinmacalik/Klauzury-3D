using System.Collections;
using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    // ---- Static: global lock so only one car reacts to enter/exit at a time ----
    public static CarEnterExit Active { get; private set; }
    static float globalEnterCooldownUntil = 0f;

    // core refs
    [SerializeField] WheelCarController carController;
    [SerializeField] CarAIDriver aiDriver;          // optional; stays off after first player drive
    [SerializeField] GameObject playerRoot;         // has PlayerMovement + its own camera
    [SerializeField] Collider enterTrigger;         // trigger around the car (isTrigger = true)

    // car-only camera (separate from the player's)
    [SerializeField] Camera carCamera;              // disabled by default; enabled while driving
    [SerializeField] Transform carCamAnchor;        // snap pose for car camera (optional)

    // reference to state to hide/show NPC driver body
    [SerializeField] CarDriverState driverState;    // optional

    // exit placement
    [SerializeField] Transform exitAnchor;          // optional exact exit spot
    [SerializeField] float exitRightMeters = 1.5f;  // fallback offsets if no anchor
    [SerializeField] float exitForwardMeters = 0.5f;
    [SerializeField] float groundRaycast = 2f;

    // keys
    [SerializeField] KeyCode enterKey = KeyCode.E;
    [SerializeField] KeyCode exitKey  = KeyCode.F;

    // post-exit braking
    [SerializeField] float postExitBrake = 0.6f;      // 0..1 (negative throttle internally)
    [SerializeField] float stopSpeedThreshold = 0.25f;
    [SerializeField] float maxBrakeTime = 1.25f;
    [SerializeField] float parkedDrag = 3.0f;          // higher drag while parked so it stays put
    [SerializeField] float parkedAngularDrag = 2.0f;
    float originalDrag, originalAngularDrag;

    // gating
    [SerializeField] float maxEnterSpeed = 1.5f;   // must be almost stopped to enter
    [SerializeField] float maxExitSpeed  = 1.0f;   // must be basically stopped to exit
    [SerializeField] float reenterBlockSeconds = 0.35f; // cooldown after exit

    // internals
    bool inCar = false;
    bool aiLockedOut = false;   // once player drives, AI won't reenable on exit
    bool playerInTrigger = false;
    float localEnterCooldownUntil = 0f; // per-car cooldown
    Coroutine brakeCo;
    SimpleGun gun;
    Rigidbody rb;                // for speed check

    void Reset()
    {
        enterTrigger = GetComponent<Collider>();
        carController = GetComponentInParent<WheelCarController>();
        aiDriver = GetComponentInParent<CarAIDriver>();
        driverState = GetComponentInParent<CarDriverState>();
    }

    void Awake()
    {
        if (enterTrigger) enterTrigger.isTrigger = true;

        if (!driverState) driverState = GetComponentInParent<CarDriverState>();

        // obtain RB for speed checks
        rb = GetComponentInParent<Rigidbody>();
        if (!rb && carController != null)
        {
            var prop = carController.GetType().GetProperty("RB");
            if (prop != null) rb = prop.GetValue(carController) as Rigidbody;
        }

        if (carCamera) carCamera.gameObject.SetActive(false);

        if (!gun && playerRoot)
            gun = playerRoot.GetComponentInChildren<SimpleGun>(true);

        // IMPORTANT: make sure idle cars ignore player input
        if (carController)
        {
            carController.SetControlMode(WheelCarController.ControlMode.External);
            carController.SetExternalInputs(0f, 0f);
        }
        
        if (rb)
        {
            originalDrag = rb.linearDamping;
            originalAngularDrag = rb.angularDamping;
        }

        // Optional: if this car should **start** as the one you drive, set inCar true and call EnterCar() from Start().
    }

    void OnTriggerEnter(Collider other)
    {
        if (!playerRoot) return;
        if (IsPlayerCollider(other)) playerInTrigger = true;
        // (Show your "Press E to enter" UI here if you like)
    }

    void OnTriggerExit(Collider other)
    {
        if (!playerRoot) return;
        if (IsPlayerCollider(other)) playerInTrigger = false;
        // (Hide the prompt UI here)
    }

    bool IsPlayerCollider(Collider other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        return root == playerRoot.transform;
    }

    void Update()
    {
        if (!playerRoot || !carController) return;

        // only the active car (or none) can react; must also be physically near THIS car
        bool canAttemptEnter = !inCar
                               && playerInTrigger
                               && Active == null
                               && Time.time >= localEnterCooldownUntil
                               && Time.time >= globalEnterCooldownUntil;

        if (canAttemptEnter && Input.GetKeyDown(enterKey))
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            if (speed <= maxEnterSpeed)
            {
                EnterCar();
                return;
            }
        }

        if (inCar && Input.GetKeyDown(exitKey))
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            if (speed <= maxExitSpeed)
            {
                ExitCar();
                return;
            }
        }
    }

    void EnterCar()
    {
        if (!playerRoot || !carController) return;

        // global lock
        Active = this;

        // lock out AI forever after the first player drive
        aiLockedOut = true;
        if (aiDriver) aiDriver.enabled = false;

        // give controls to player
        carController.SetControlMode(WheelCarController.ControlMode.Player);
        carController.SetExternalInputs(0f, 0f); // clear any lingering external input

        // Hide the NPC body immediately when we take the seat
        if (driverState) driverState.HideDriverBody();

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
        
        // restore normal physics (unpark)
        if (rb)
        {
            rb.linearDamping = originalDrag;
            rb.angularDamping = originalAngularDrag;
        }

        // take control
        carController.SetControlMode(WheelCarController.ControlMode.Player);
        carController.SetExternalInputs(0f, 0f); // clear any lingering external input
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
            // default: right side, slightly forward, snap to ground
            var basePos = carController.transform.position
                          + carController.transform.right * exitRightMeters
                          + carController.transform.forward * exitForwardMeters;

            if (Physics.Raycast(new Ray(basePos + Vector3.up * groundRaycast, Vector3.down),
                                out var hit, groundRaycast * 2f, ~0, QueryTriggerInteraction.Ignore))
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
            brakeCo = StartCoroutine(PostExitBrakeThenPark());
        }

        inCar = false;

        // release global lock + start small cooldown so we don't instantly snap into another car
        Active = null;
        localEnterCooldownUntil = Time.time + reenterBlockSeconds;
        globalEnterCooldownUntil = Time.time + reenterBlockSeconds;

        // also, mark we're no longer inside trigger until physics says so (prevents single-frame re-entry)
        playerInTrigger = false;
        
        // hand off control to External and CLEAR inputs right away
        carController.SetControlMode(WheelCarController.ControlMode.External);
        carController.SetExternalInputs(0f, 0f);

        // start a brief braking phase → then park
        if (brakeCo != null) StopCoroutine(brakeCo);
        brakeCo = StartCoroutine(PostExitBrakeThenPark());
    }

    IEnumerator PostExitBrakeThenPark()
    {
        float t = 0f;

        // Phase 1: gentle braking until stopped or timeout
        while (t < maxBrakeTime)
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;  // <- use velocity, not linearVelocity
            if (speed <= stopSpeedThreshold) break;

            // negative "throttle" as a brake, no steering
            carController.SetExternalInputs(-Mathf.Clamp01(postExitBrake), 0f);

            t += Time.deltaTime;
            yield return null;
        }

        // Neutralize inputs
        carController.SetExternalInputs(0f, 0f);

        // Phase 2: park — increase drag so it stays put, and zero out motion
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = parkedDrag;
            rb.angularDamping = parkedAngularDrag;
        }
    }


    // --- helpers ---

    void HardLockWeapon()
    {
        WeaponHotkeys.GunIsReady = false;

        if (gun && gun.TryGetComponent<Animator>(out var a))
        {
            a.ResetTrigger("Fire");
            a.SetBool("IsADS", false);
            a.SetBool("IsReady", false);
        }
    }
}
