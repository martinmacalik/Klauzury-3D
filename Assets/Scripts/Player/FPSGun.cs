using UnityEngine;

public class SimpleGun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera playerCam;
    [SerializeField] Animator animator;              // Has IsADS (bool) + Fire (trigger)
    [SerializeField] ParticleSystem muzzleFlash;     // optional
    [SerializeField] GameObject hitFxPrefab;         // optional

    [Header("ADS")]
    [SerializeField] float hipFOV = 60f;
    [SerializeField] float adsFOV = 45f;
    [SerializeField] float fovLerpSpeed = 10f;

    [Header("Fire")]
    [SerializeField] float fireRate = 10f;           // bullets per second
    [SerializeField] float damage = 25f;
    [SerializeField] float range = 200f;
    [SerializeField] LayerMask hitMask = ~0;

    [Tooltip("Hip-fire spread in degrees.")]
    [SerializeField] float hipSpread = 1.2f;
    [Tooltip("ADS spread in degrees.")]
    [SerializeField] float adsSpread = 0.2f;

    [Header("Sync options")]
    [Tooltip("If true, do the actual shot from an Animation Event calling DoShoot().")]
    [SerializeField] bool fireViaAnimEvent = false;

    float nextFireTime;

    void Start()
    {
        if (playerCam) playerCam.fieldOfView = hipFOV;
    }

    void Update()
    {
        HandleADS();
        HandleInputFire();
        LerpFOV();
    }

    void HandleADS()
    {
        // Hold right mouse for ADS (change to toggle if you prefer)
        bool isADS = Input.GetMouseButton(1);
        animator.SetBool("IsADS", isADS);
    }

    void HandleInputFire()
    {
        if (!fireViaAnimEvent)
        {
            // Auto fire (hold LMB). For semi-auto use GetMouseButtonDown(0).
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + (1f / fireRate);
                animator.SetTrigger("Fire");
                // We’ll shoot immediately here (no anim event)
                DoShoot();
            }
        }
        else
        {
            // When using anim events, only gate the rate & trigger the anim here.
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + (1f / fireRate);
                animator.SetTrigger("Fire");
                // Actual DoShoot() will be called by the Animation Event.
            }
        }
    }

    void LerpFOV()
    {
        bool isADS = animator.GetBool("IsADS");
        float target = isADS ? adsFOV : hipFOV;
        if (playerCam) playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, target, Time.deltaTime * fovLerpSpeed);
    }

    // === Called either directly after triggering Fire, or via Animation Event ===
    public void DoShoot()
    {
        bool isADS = animator.GetBool("IsADS");
        float spreadDeg = isADS ? adsSpread : hipSpread;

        if (muzzleFlash) muzzleFlash.Play();

        Vector3 dir = GetSpreadDirection(spreadDeg);
        if (Physics.Raycast(playerCam.transform.position, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Optional damage hook
            var dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(damage);

            if (hitFxPrefab)
            {
                var fx = Instantiate(hitFxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 5f);
            }
        }
    }

    Vector3 GetSpreadDirection(float spreadDegrees)
    {
        float s = spreadDegrees * Mathf.Deg2Rad;
        float rx = Random.Range(-s, s);
        float ry = Random.Range(-s, s);
        Transform t = playerCam.transform;
        Vector3 dir = (t.forward + t.right * rx + t.up * ry).normalized;
        return dir;
    }
}

// Optional
public interface IDamageable { void TakeDamage(float amount); }
