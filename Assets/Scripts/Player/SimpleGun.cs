using UnityEngine;

public class SimpleGun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera playerCam;
    [SerializeField] Animator animator;
    [Tooltip("Tip of the barrel: used for muzzle flash & tracer start.")]
    [SerializeField] Transform muzzle;
    [Tooltip("Optional: a ParticleSystem already placed under the muzzle.")]
    [SerializeField] GameObject muzzleFlashPrefab; // optional prefab alternative

    [Header("Shooting")]
    [SerializeField] float range = 150f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float hipSpread = 1.25f;
    [SerializeField] float adsSpread = 0.35f;
    [SerializeField] float fireCooldown = 0.12f;
    float nextFireTime;

    [Header("ADS")]
    [SerializeField] float adsFov = 55f;
    [SerializeField] float hipFov = 70f;
    [SerializeField] float fovLerp = 14f;

    [Header("Hit FX")]
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] float hitFxLifetime = 5f;
    
    [Header("Tracer FX")]
    [SerializeField] GameObject tracerPrefab;    // assign TracerRay prefab
    [SerializeField] float tracerSpeed = 250f;   // meters/sec (visual speed)
    [SerializeField] float tracerStay = 0.04f;   // how long it lingers after impact


    // Animator hashes
    static readonly int Hash_IsADS   = Animator.StringToHash("IsADS");
    static readonly int Hash_Fire    = Animator.StringToHash("Fire");
    static readonly int Hash_IsReady = Animator.StringToHash("IsReady");

    void Awake()
    {
        if (!playerCam) Debug.LogError("SimpleGun: playerCam not set", this);
        if (!animator) Debug.LogWarning("SimpleGun: animator not set", this);
        if (!muzzle)   Debug.LogWarning("SimpleGun: muzzle not set – using camera fallback.", this);
    }

    void Update()
    {
        // Authoritative lock check from WeaponHotkeys
        bool isLocked = !WeaponHotkeys.GunIsReady;

        if (isLocked)
        {
            if (animator)
            {
                animator.ResetTrigger(Hash_Fire);
                animator.SetBool(Hash_IsADS, false);
                animator.SetBool("IsReady", false);
            }
            LerpFOV(false);
            return;
        }

        if (animator) animator.SetBool("IsReady", true);

        bool wantADS = Input.GetMouseButton(1);
        if (animator) animator.SetBool(Hash_IsADS, wantADS);
        LerpFOV(wantADS);

        if (Input.GetMouseButton(0))
            TryFire();
    }

    void LerpFOV(bool wantADS)
    {
        if (!playerCam) return;
        float targetFov = wantADS ? adsFov : hipFov;
        playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFov, Time.deltaTime * fovLerp);
    }

    void TryFire()
    {
        if (!WeaponHotkeys.GunIsReady) return;
        if (Time.time < nextFireTime) return;
        if (!playerCam) return;

        nextFireTime = Time.time + fireCooldown;

        // --- MUZZLE FLASH ---
        if (!muzzle)
        {
            Debug.LogError("SimpleGun: No muzzle assigned — cannot place muzzle flash.", this);
        }
        else if (muzzleFlashPrefab)
        {
            // Spawn as a child of the muzzle so it always aligns & follows recoil this frame
            var go = Instantiate(muzzleFlashPrefab, muzzle, false); // local pos/rot = (0,0,0)
    
            // Ensure all systems use Local space (so the flash sticks to the gun)
            float life = 0.25f;
            var psAll = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psAll)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                // accumulate a safe lifetime for cleanup
                life = Mathf.Max(life, main.duration + main.startLifetimeMultiplier);
                ps.Play(true);
            }

            // Auto-destroy after it finishes
            Destroy(go, life);
        }



        if (animator) animator.SetTrigger(Hash_Fire);

        // Shoot immediately (or let anim event call DoShoot if you prefer timing)
        DoShoot();
    }

    public void DoShoot()
    {
        if (!WeaponHotkeys.GunIsReady || !playerCam) return;

        bool isADS   = animator ? animator.GetBool(Hash_IsADS) : false;
        float spread = isADS ? adsSpread : hipSpread;

        // 1) Camera aim ray (crosshair)
        Ray aimRay = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimDir = ApplySpread(aimRay.direction, spread, playerCam.transform);

        Vector3 targetPoint;
        RaycastHit camHit;

        // 2) Camera decides what we hit
        if (Physics.Raycast(aimRay.origin, aimDir, out camHit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            //camHit.collider.GetComponent<IDamageable>()?.TakeDamage(1);
            targetPoint = camHit.point;
        }
        else
        {
            targetPoint = aimRay.origin + aimDir * range;
        }

        // 3) Barrel obstruction check
        Vector3 muzzlePos = muzzle ? muzzle.position : aimRay.origin;
        if (muzzle)
        {
            if (Physics.Linecast(muzzlePos, targetPoint, out var barrelHit, hitMask, QueryTriggerInteraction.Ignore))
            {
                targetPoint = barrelHit.point;
                //barrelHit.collider.GetComponent<IDamageable>()?.TakeDamage(1);
            }
        }

        // 4) Impact FX (if something was hit)
        Vector3 vfxDir = (targetPoint - muzzlePos).normalized;
        if (hitFxPrefab)
        {
            if (Physics.Raycast(muzzlePos, vfxDir, out var fxHit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                var rot = Quaternion.LookRotation(fxHit.normal);
                var fx  = Instantiate(hitFxPrefab, fxHit.point, rot);
                Destroy(fx, hitFxLifetime);
            }
        }
        // 5) Tracer FX
        SpawnTracer(muzzlePos, targetPoint);
    }

    Vector3 ApplySpread(Vector3 forward, float spreadDeg, Transform basis)
    {
        if (spreadDeg <= 0f) return forward;
        float yaw   = Random.Range(-spreadDeg, spreadDeg);
        float pitch = Random.Range(-spreadDeg, spreadDeg);
        Quaternion q = Quaternion.AngleAxis(pitch, basis.right) * Quaternion.AngleAxis(yaw, basis.up);
        return (q * forward).normalized;
    }
    
    void SpawnTracer(Vector3 start, Vector3 end)
{
    // One-frame editor line so you can see where it *should* be
    Debug.DrawLine(start, end, Color.yellow, 0.05f);

    LineRenderer lr = null;

    if (tracerPrefab)
    {
        var go = Instantiate(tracerPrefab);
        lr = go.GetComponent<LineRenderer>();
        if (!lr) lr = go.AddComponent<LineRenderer>();
    }
    else
    {
        // Build a minimal tracer at runtime
        var go = new GameObject("TracerRay (auto)");
        lr = go.AddComponent<LineRenderer>();
    }

    // Ensure visible settings
    lr.useWorldSpace = true;
    lr.positionCount = 2;
    lr.alignment = LineAlignment.View; // faces camera
    lr.textureMode = LineTextureMode.Stretch;
    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    lr.receiveShadows = false;
    lr.numCapVertices = 0;
    lr.numCornerVertices = 0;

    // Width you can actually see
    float w = 0.02f; // ~2 cm at world scale
    lr.startWidth = w;
    lr.endWidth   = w;

    // Make sure it has a material and visible color (URP/HDRP safe-ish)
    if (!lr.sharedMaterial)
    {
        Shader sh =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("HDRP/Unlit") ??
            Shader.Find("Unlit/Color");
        var mat = new Material(sh);
        if (sh.name.Contains("Unlit"))
        {
            // set a bright color property if available
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        }
        lr.sharedMaterial = mat;
    }
    // For Unlit/Color fallback:
    if (lr.sharedMaterial.HasProperty("_Color"))
        lr.sharedMaterial.SetColor("_Color", Color.white);

    // Place at start, then animate tip to end
    lr.SetPosition(0, start);
    lr.SetPosition(1, start);
    StartCoroutine(AnimateTracer(lr, start, end, tracerSpeed, tracerStay));
}

System.Collections.IEnumerator AnimateTracer(LineRenderer lr, Vector3 start, Vector3 end, float speed, float stay)
{
    float dist = Vector3.Distance(start, end);
    float travelTime = Mathf.Max(0.001f, dist / Mathf.Max(1f, tracerSpeed));
    float t = 0f;

    while (t < 1f && lr)
    {
        t += Time.deltaTime / travelTime;
        Vector3 tip = Vector3.Lerp(start, end, t);
        lr.SetPosition(0, start);
        lr.SetPosition(1, tip);
        yield return null;
    }

    if (lr && stay > 0f) yield return new WaitForSeconds(stay);
    if (lr) Destroy(lr.gameObject);
}


}
