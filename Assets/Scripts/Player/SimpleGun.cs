using System.Collections;
using UnityEngine;

public class SimpleGun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera playerCam;
    [SerializeField] Animator animator;
    [Tooltip("Tip of the barrel: used for muzzle flash & tracer start.")]
    [SerializeField] Transform muzzle;
    [Tooltip("Muzzle flash PREFAB (instantiated as a child of the muzzle).")]
    [SerializeField] GameObject muzzleFlashPrefab;

    [Header("Shooting")]
    [SerializeField] float range = 150f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float hipSpread = 1.25f;
    [SerializeField] float adsSpread = 0.35f;
    [SerializeField] float fireCooldown = 0.12f;
    [SerializeField] int damage = 10;               // <-- public in Inspector
    float nextFireTime;

    [Header("ADS")]
    [SerializeField] float adsFov = 55f;
    [SerializeField] float hipFov = 70f;
    [SerializeField] float fovLerp = 14f;

    [Header("Hit FX")]
    [SerializeField] GameObject hitFxPrefab;
    [SerializeField] float hitFxLifetime = 5f;

    [Header("Tracer FX")]
    [SerializeField] GameObject tracerPrefab;     // optional LineRenderer prefab
    [SerializeField] float tracerSpeed = 300f;    // m/s (visual)
    [SerializeField] float tracerStay = 0.04f;    // linger time

    // Animator hashes
    static readonly int Hash_IsADS   = Animator.StringToHash("IsADS");
    static readonly int Hash_Fire    = Animator.StringToHash("Fire");
    static readonly int Hash_IsReady = Animator.StringToHash("IsReady");

    void Awake()
    {
        if (!playerCam) Debug.LogError("SimpleGun: playerCam not set", this);
        if (!animator) Debug.LogWarning("SimpleGun: animator not set", this);
        if (!muzzle)   Debug.LogWarning("SimpleGun: muzzle not set – assign a barrel tip.", this);
    }

    void Update()
    {
        bool isLocked = !WeaponHotkeys.GunIsReady;

        if (isLocked)
        {
            if (animator)
            {
                animator.ResetTrigger(Hash_Fire);
                animator.SetBool(Hash_IsADS, false);
                animator.SetBool(Hash_IsReady, false);
            }
            LerpFOV(false);
            return;
        }

        if (animator) animator.SetBool(Hash_IsReady, true);

        bool wantADS = Input.GetMouseButton(1);
        if (animator) animator.SetBool(Hash_IsADS, wantADS);
        LerpFOV(wantADS);

        // No fire from Update — TryFire() will enforce ADS requirement.
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
        if (Time.time < nextFireTime)  return;
        if (!playerCam)                return;

        // >>> MUST be in ADS to shoot <<<
        bool isADS = animator ? animator.GetBool(Hash_IsADS) : Input.GetMouseButton(1);
        if (!isADS) return;

        nextFireTime = Time.time + fireCooldown;

        // --- MUZZLE FLASH ---
        if (!muzzle)
        {
            Debug.LogError("SimpleGun: No muzzle assigned — cannot place muzzle flash.", this);
        }
        else if (muzzleFlashPrefab)
        {
            var go = Instantiate(muzzleFlashPrefab, muzzle, false); // local 0,0,0
            float life = 0.25f;
            var psAll = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psAll)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                life = Mathf.Max(life, main.duration + main.startLifetimeMultiplier);
                ps.Play(true);
            }
            Destroy(go, life);
        }

        if (animator) animator.SetTrigger(Hash_Fire);

        // Shoot immediately (or gate by an animation event if you prefer)
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

        // 2) Camera decides what we aim at
        if (Physics.Raycast(aimRay.origin, aimDir, out camHit, range, hitMask, QueryTriggerInteraction.Ignore))
            targetPoint = camHit.point;
        else
            targetPoint = aimRay.origin + aimDir * range;

        // 3) Barrel obstruction check
        Vector3 muzzlePos = muzzle ? muzzle.position : aimRay.origin;
        if (muzzle)
        {
            if (Physics.Linecast(muzzlePos, targetPoint, out var barrelHit, hitMask, QueryTriggerInteraction.Ignore))
                targetPoint = barrelHit.point;
        }

        // 4) Tracer (visual)
        SpawnTracer(muzzlePos, targetPoint);

        // 5) Real hit ray from the muzzle (authoritative impact)
        Vector3 vfxDir = (targetPoint - muzzlePos).normalized;
        if (Physics.Raycast(muzzlePos, vfxDir, out var fxHit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Impact VFX
            if (hitFxPrefab)
            {
                var rot = Quaternion.LookRotation(fxHit.normal);
                var fx  = Instantiate(hitFxPrefab, fxHit.point, rot);
                Destroy(fx, hitFxLifetime);
            }

            // Damage via interface — uses serialized 'damage'
            if (fxHit.collider.TryGetComponent<DamageHitbox>(out var box))
                box.ApplyDamage(damage);
            else
                fxHit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage);

            // Optional physics impulse
            if (fxHit.rigidbody)
                fxHit.rigidbody.AddForceAtPosition(vfxDir * 10f, fxHit.point, ForceMode.Impulse);
        }
    }

    Vector3 ApplySpread(Vector3 forward, float spreadDeg, Transform basis)
    {
        if (spreadDeg <= 0f) return forward;
        float yaw   = Random.Range(-spreadDeg, spreadDeg);
        float pitch = Random.Range(-spreadDeg, spreadDeg);
        Quaternion q = Quaternion.AngleAxis(pitch, basis.right) * Quaternion.AngleAxis(yaw, basis.up);
        return (q * forward).normalized;
    }

    // --------- Tracer ----------
    void SpawnTracer(Vector3 start, Vector3 end)
    {
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
            var go = new GameObject("TracerRay (auto)");
            lr = go.AddComponent<LineRenderer>();
        }

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        float w = 0.02f;
        lr.startWidth = w;
        lr.endWidth   = w;

        if (!lr.sharedMaterial)
        {
            Shader sh =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("HDRP/Unlit") ??
                Shader.Find("Unlit/Color");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            lr.sharedMaterial = mat;
        }

        lr.SetPosition(0, start);
        lr.SetPosition(1, start);
        StartCoroutine(AnimateTracer(lr, start, end, tracerSpeed, tracerStay));
    }

    IEnumerator AnimateTracer(LineRenderer lr, Vector3 start, Vector3 end, float speed, float stay)
    {
        float dist = Vector3.Distance(start, end);
        float travelTime = Mathf.Max(0.001f, dist / Mathf.Max(1f, speed));
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (muzzle)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(muzzle.position, 0.01f);
            Gizmos.DrawRay(muzzle.position, muzzle.forward * 0.2f);
        }
    }
#endif
}
