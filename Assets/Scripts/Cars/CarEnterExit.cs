using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    // ---- Static: global lock so only one car reacts to enter/exit at a time ----
    public static CarEnterExit Active { get; private set; }
    static float globalEnterCooldownUntil = 0f;

    // Optional manual overrides (assign in Inspector if auto-find fails)
    [SerializeField] GameObject manualPlayerOverride;
    [SerializeField] Camera manualCarCameraOverride;

    // core refs
    [SerializeField] WheelCarController carController;
    [SerializeField] CarAIDriver aiDriver;          // optional; stays off after first player drive
    [SerializeField] GameObject playerRoot;         // has PlayerMovement + its own camera (assign manually if auto-find fails)
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
    
    // layer filtering
    [SerializeField] LayerMask playerLayerMask = -1; // Set to "Player" layer in inspector

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
        
        // Auto-configure player layer mask if it's set to "Everything" (-1) or "Nothing" (0)
        if (playerLayerMask.value == -1 || playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                playerLayerMask = 1 << playerLayer;
                Debug.Log($"[CarEnterExit] Auto-configured player layer mask to layer {playerLayer} (mask value: {playerLayerMask.value})");
            }
            else
            {
                // If Player layer doesn't exist, use Default layer as fallback
                playerLayerMask = 1 << LayerMask.NameToLayer("Default");
                Debug.LogWarning($"[CarEnterExit] Player layer not found, using Default layer instead (mask value: {playerLayerMask.value})");
            }
        }

        // Use manual override first if provided
        if (manualPlayerOverride != null && !playerRoot)
        {
            playerRoot = manualPlayerOverride;
            Debug.Log($"[CarEnterExit] Using manual player override: {playerRoot.name}");
        }
        
        // Auto-find player by tag if not assigned
        if (!playerRoot)
        {
            // Try finding by tag first - most reliable method
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                playerRoot = taggedPlayer;
                Debug.Log($"[CarEnterExit] Found player by tag: {playerRoot.name}");
            }
            else
            {
                // Fallback: search all GameObjects for one with Player tag on any layer
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.CompareTag("Player"))
                    {
                        playerRoot = obj;
                        Debug.Log($"[CarEnterExit] Found player by searching all objects: {playerRoot.name}");
                        break;
                    }
                }
                
                // Last resort: try by name
                if (playerRoot == null)
                {
                    playerRoot = GameObject.Find("Player");
                    if (playerRoot == null)
                        playerRoot = GameObject.Find("PlayerRoot");
                    
                    if (playerRoot != null)
                        Debug.Log($"[CarEnterExit] Found player by name: {playerRoot.name}");
                }
            }
            
            if (playerRoot)
            {
                gun = playerRoot.GetComponentInChildren<SimpleGun>(true);
            }
            else
            {
                Debug.LogError("[CarEnterExit] CRITICAL: Could not find player object in any way!");
            }
        }
        
        // Auto-find car camera by tag if not assigned (including inactive objects)
        if (!carCamera)
        {
            Debug.Log("[CarEnterExit] Searching for CarCamera...");
            
            // Use FindObjectsByType with IncludeInactive to work in builds
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            Debug.Log($"[CarEnterExit] Found {allCameras.Length} cameras total");
            
            foreach (Camera cam in allCameras)
            {
                Debug.Log($"[CarEnterExit] Checking camera: {cam.name}, tag: {cam.tag}, active: {cam.gameObject.activeSelf}");
                
                if (cam.CompareTag("CarCamera"))
                {
                    carCamera = cam;
                    Debug.Log($"[CarEnterExit] Found CarCamera: {cam.name}");
                    if (carCamera.gameObject.activeSelf)
                    {
                        carCamera.gameObject.SetActive(false);
                        Debug.Log("[CarEnterExit] Disabled CarCamera (was active)");
                    }
                    break;
                }
            }
            
            if (!carCamera)
            {
                Debug.LogError("[CarEnterExit] CRITICAL: Could not find camera with 'CarCamera' tag!");
            }
        }

        // obtain RB for speed checks
        rb = GetComponentInParent<Rigidbody>();
        if (!rb && carController != null)
        {
            var prop = carController.GetType().GetProperty("RB");
            if (prop != null) rb = prop.GetValue(carController) as Rigidbody;
        }

        if (rb)
        {
            originalDrag = rb.linearDamping;
            originalAngularDrag = rb.angularDamping;
        }

        // Optional: if this car should **start** as the one you drive, set inCar true and call EnterCar() from Start().
    }

    void Start()
    {
        StartCoroutine(InitializeWithRetry());
    }
    
    IEnumerator InitializeWithRetry()
    {
        // Critical checks for build compatibility
        Debug.Log($"[CarEnterExit] {gameObject.name} Start() - Beginning initialization checks...");
        
        // Wait one frame to ensure all objects are loaded
        yield return null;
        
        // Retry finding player if not found (up to 3 attempts with delays)
        int playerRetries = 0;
        while (!playerRoot && playerRetries < 3)
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Retry {playerRetries + 1} - Searching for player...");
            
            // Try FindGameObjectWithTag
            GameObject taggedPlayer = null;
            try
            {
                taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CarEnterExit] FindGameObjectWithTag failed: {e.Message}");
            }
            
            if (taggedPlayer != null)
            {
                playerRoot = taggedPlayer;
                Debug.Log($"[CarEnterExit] Found player by tag: {playerRoot.name}");
            }
            else
            {
                // Try FindObjectsByType
                PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (allPlayers != null && allPlayers.Length > 0)
                {
                    playerRoot = allPlayers[0].gameObject;
                    Debug.Log($"[CarEnterExit] Found player by component search: {playerRoot.name}");
                }
            }
            
            if (!playerRoot)
            {
                playerRetries++;
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (!playerRoot)
        {
            Debug.LogError($"[CarEnterExit] {gameObject.name}: Player not found after retries! Car cannot be entered. Make sure Player object has 'Player' tag.");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Player found: {playerRoot.name}, tag: {playerRoot.tag}, layer: {LayerMask.LayerToName(playerRoot.layer)}");
            gun = playerRoot.GetComponentInChildren<SimpleGun>(true);
        }
        
        // Check manual camera override first
        if (manualCarCameraOverride != null && !carCamera)
        {
            carCamera = manualCarCameraOverride;
            Debug.Log($"[CarEnterExit] Using manual car camera override in retry: {carCamera.name}");
        }
        
        // Retry finding car camera if not found
        int cameraRetries = 0;
        while (!carCamera && cameraRetries < 3)
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Retry {cameraRetries + 1} - Searching for CarCamera...");
            
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[CarEnterExit] Found {allCameras.Length} cameras in retry {cameraRetries + 1}");
            
            foreach (Camera cam in allCameras)
            {
                try
                {
                    if (cam.CompareTag("CarCamera"))
                    {
                        carCamera = cam;
                        Debug.Log($"[CarEnterExit] Found CarCamera: {cam.name}");
                        if (carCamera.gameObject.activeSelf)
                        {
                            carCamera.gameObject.SetActive(false);
                        }
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[CarEnterExit] Error checking camera tag on {cam.name}: {e.Message}");
                }
            }
            
            if (!carCamera)
            {
                cameraRetries++;
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (!carCamera)
        {
            Debug.LogError($"[CarEnterExit] {gameObject.name}: Car camera not found after retries! Car cannot be entered. Make sure there's a Camera with 'CarCamera' tag.");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Car camera found: {carCamera.name}");
        }
        
        if (!carController)
        {
            Debug.LogError($"[CarEnterExit] {gameObject.name}: Car controller not assigned!");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Car controller found: {carController.name}");
        }
        
        if (!enterTrigger)
        {
            Debug.LogWarning($"[CarEnterExit] {gameObject.name}: Enter trigger not assigned!");
        }
        else if (!enterTrigger.isTrigger)
        {
            Debug.LogWarning($"[CarEnterExit] {gameObject.name}: Enter trigger is not marked as trigger!");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Enter trigger configured: {enterTrigger.name}");
        }
        
        // Check player layer configuration
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer == -1)
        {
            Debug.LogError($"[CarEnterExit] {gameObject.name}: 'Player' layer does not exist in project! Please add it in Layer settings.");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Player layer exists: {playerLayer}");
        }
        
        if (playerLayerMask.value == -1 || playerLayerMask.value == 0)
        {
            Debug.LogWarning($"[CarEnterExit] {gameObject.name}: Player layer mask not configured properly! Value: {playerLayerMask.value}");
        }
        else
        {
            Debug.Log($"[CarEnterExit] {gameObject.name}: Player layer mask configured: {playerLayerMask.value}");
        }
        
        // Ensure AI driver is enabled on start if it exists and hasn't been locked out
        if (aiDriver && !aiLockedOut && !inCar)
        {
            aiDriver.enabled = true;
            Debug.Log($"[CarEnterExit] {gameObject.name}: AI driver enabled");
        }
        
        Debug.Log($"[CarEnterExit] {gameObject.name} Start() - Initialization complete");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!playerRoot) return;
        
        // FIRST: Check if this collider is on the player layer mask
        int layer = other.gameObject.layer;
        if ((playerLayerMask.value & (1 << layer)) == 0)
        {
            // Not on player layer, ignore completely (no logs)
            return;
        }
        
        Debug.Log($"[CarEnterExit] ============ TRIGGER ENTER ============");
        Debug.Log($"[CarEnterExit] Collider: {other.name}");
        Debug.Log($"[CarEnterExit] Tag: {other.tag}");
        Debug.Log($"[CarEnterExit] GameObject: {other.gameObject.name}");
        Debug.Log($"[CarEnterExit] Root: {other.transform.root.name}");
        Debug.Log($"[CarEnterExit] Layer: {LayerMask.LayerToName(layer)} (passed layer mask check!)");
        Debug.Log($"[CarEnterExit] PlayerRoot: {playerRoot.name}");
        
        Debug.Log($"[CarEnterExit] Checking if this is player...");
        if (IsPlayerCollider(other))
        {
            playerInTrigger = true;
            Debug.Log($"[CarEnterExit] ✓✓✓ PLAYER ENTERED TRIGGER ZONE! playerInTrigger={playerInTrigger} ✓✓✓");
        }
        else
        {
            Debug.Log($"[CarEnterExit] ✗✗✗ NOT PLAYER ✗✗✗");
        }
        Debug.Log($"[CarEnterExit] =======================================");
    }

    void OnTriggerExit(Collider other)
    {
        if (!playerRoot) return;
        
        // FIRST: Check if this collider is on the player layer mask
        int layer = other.gameObject.layer;
        if ((playerLayerMask.value & (1 << layer)) == 0)
        {
            // Not on player layer, ignore completely
            return;
        }
        
        Debug.Log($"[CarEnterExit] OnTriggerExit: {other.name} (layer: {LayerMask.LayerToName(layer)})");
        
        if (IsPlayerCollider(other))
        {
            playerInTrigger = false;
            Debug.Log($"[CarEnterExit] Player exited trigger zone! playerInTrigger={playerInTrigger}");
        }
    }

    bool IsPlayerCollider(Collider other)
    {
        if (!playerRoot)
        {
            Debug.LogWarning("[CarEnterExit] IsPlayerCollider called but playerRoot is null!");
            return false;
        }
        
        // Check 1: Layer comparison (most reliable when layer is set correctly)
        string layerName = LayerMask.LayerToName(other.gameObject.layer);
        if (layerName == "Player")
        {
            Debug.Log($"[CarEnterExit] ✓ Layer match: {other.name} is on Player layer");
            return true;
        }
        
        // Check 2: Direct GameObject match
        if (other.gameObject == playerRoot)
        {
            Debug.Log($"[CarEnterExit] ✓ Direct match: {other.name}");
            return true;
        }
        
        // Check 3: Tag comparison
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[CarEnterExit] ✓ Tag match: {other.name} has Player tag");
            return true;
        }
        
        // Check 4: Is the collider a child of playerRoot?
        Transform current = other.transform;
        while (current != null)
        {
            if (current.gameObject == playerRoot)
            {
                Debug.Log($"[CarEnterExit] ✓ Hierarchy match: {other.name} is child of {playerRoot.name}");
                return true;
            }
            current = current.parent;
        }
        
        // Check 5: Transform root match (with Rigidbody)
        var rootWithRb = other.attachedRigidbody ? other.attachedRigidbody.transform.root : other.transform.root;
        if (rootWithRb == playerRoot.transform)
        {
            Debug.Log($"[CarEnterExit] ✓ Root match (with RB): {other.name} -> {rootWithRb.name}");
            return true;
        }
        
        // Check 6: Transform root match (without Rigidbody consideration)
        var rootDirect = other.transform.root;
        if (rootDirect == playerRoot.transform)
        {
            Debug.Log($"[CarEnterExit] ✓ Direct root match: {other.name} -> {rootDirect.name}");
            return true;
        }
        
        Debug.Log($"[CarEnterExit] ✗ NOT player: {other.name}, root: {rootDirect.name}, tag: {other.tag}, layer: {layerName}, playerRoot: {playerRoot.name}");
        return false;
    }

    void Update()
    {

        // IMPORTANT: make sure idle cars ignore player input
        if (carController && !inCar)
        {
            carController.SetControlMode(WheelCarController.ControlMode.External);
            carController.SetExternalInputs(0f, 0f);
        }

        if (!playerRoot || !carController) return;

        // only the active car (or none) can react; must also be physically near THIS car
        bool canAttemptEnter = !inCar
                               && playerInTrigger
                               && Active == null
                               && Time.time >= localEnterCooldownUntil
                               && Time.time >= globalEnterCooldownUntil;

        if (Input.GetKeyDown(enterKey))
        {
            if (!canAttemptEnter)
            {
                Debug.Log($"[CarEnterExit] Cannot enter: inCar={inCar}, playerInTrigger={playerInTrigger}, Active={(Active != null ? Active.name : "null")}, localCooldown={Time.time >= localEnterCooldownUntil}, globalCooldown={Time.time >= globalEnterCooldownUntil}");
            }
            else
            {
                // Enter car immediately without speed check
                Debug.Log($"[CarEnterExit] Entering car {name}...");
                EnterCar();
                return;
            }
        }

        if (inCar && Input.GetKeyDown(exitKey))
        {
            // Exit car immediately without speed check
            ExitCar();
            return;
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

        // Check speed FIRST before changing anything
        float currentSpeed = rb ? rb.linearVelocity.magnitude : 0f;
        bool shouldBrake = currentSpeed > stopSpeedThreshold;
        
        Debug.Log($"[CarEnterExit] Exiting car at {currentSpeed:F2} m/s - will brake: {shouldBrake}");
        
        // Clear inputs and switch to external control
        carController.SetControlMode(WheelCarController.ControlMode.External);
        carController.SetExternalInputs(0f, 0f);

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

        // Apply high drag immediately to help slow down
        if (rb)
        {
            rb.linearDamping = parkedDrag;
            rb.angularDamping = parkedAngularDrag;
        }

        // do NOT re-enable AI if we've ever driven this car
        if (aiDriver && !aiLockedOut)
        {
            aiDriver.enabled = true;
            carController.SetControlMode(WheelCarController.ControlMode.External);
            carController.SetExternalInputs(0f, 0f);
        }
        else
        {
            // Start braking coroutine if car was moving
            if (shouldBrake)
            {
                if (brakeCo != null) StopCoroutine(brakeCo);
                brakeCo = StartCoroutine(PostExitBrakeThenPark());
            }
        }

        inCar = false;

        // release global lock + start small cooldown so we don't instantly snap into another car
        Active = null;
        localEnterCooldownUntil = Time.time + reenterBlockSeconds;
        globalEnterCooldownUntil = Time.time + reenterBlockSeconds;

        // also, mark we're no longer inside trigger until physics says so (prevents single-frame re-entry)
        playerInTrigger = false;
    }

    IEnumerator PostExitBrakeThenPark()
    {
        float t = 0f;

        // Phase 1: gentle braking until stopped or timeout
        while (t < maxBrakeTime)
        {
            float speed = rb ? rb.linearVelocity.magnitude : 0f;
            if (speed <= stopSpeedThreshold) break;

            // negative "throttle" as a brake, no steering
            carController.SetExternalInputs(-Mathf.Clamp01(postExitBrake), 0f);

            t += Time.deltaTime;
            yield return null;
        }

        // Neutralize inputs and let the high drag finish the job
        carController.SetExternalInputs(0f, 0f);
        
        Debug.Log("[CarEnterExit] Braking complete - car will coast to stop with high drag");
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
