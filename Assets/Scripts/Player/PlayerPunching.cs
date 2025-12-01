using UnityEngine;

public class PlayerPunching : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private KeyCode punchKey = KeyCode.Mouse0;
    [SerializeField] private float punchDamage = 25f;
    [SerializeField] private float punchRange = 2f;
    [SerializeField] private LayerMask npcLayer;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string punchAnimationTrigger = "Punch";
    
    [Header("Cooldown")]
    [SerializeField] private float punchCooldown = 0.5f;
    private float lastPunchTime = -999f;
    
    [Header("Damage Timing")]
    [SerializeField] private float damageDelay = 0.3f; // Time after animation starts to deal damage
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    
    private bool canPunch = true;
    private int totalPunches = 0;

    void Start()
    {
        // Auto-find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (!canPunch) return;
        
        // Check for punch input - can punch at any time, even when gun is aimed
        if (Input.GetKeyDown(punchKey) && Time.time >= lastPunchTime + punchCooldown)
        {
            StartPunch();
        }
    }

    void StartPunch()
    {
        if (animator != null)
        {
            animator.SetTrigger(punchAnimationTrigger);
            lastPunchTime = Time.time;
            
            // Schedule damage dealing after delay
            Invoke(nameof(DealPunchDamage), damageDelay);
        }
        else
        {
            Debug.LogWarning("[PlayerPunching] No animator assigned!");
        }
    }

    // This method will be called from the animation event at the right frame
    public void DealPunchDamage()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("[PlayerPunching] No camera assigned!");
            return;
        }

        // Raycast from camera center
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, punchRange, npcLayer))
        {
            Debug.Log($"[PlayerPunching] Hit {hit.collider.name} with punch!");

            // Try to find IDamageable component on the NPC
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.collider.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage((int)punchDamage);
                Debug.Log($"[PlayerPunching] Dealt {punchDamage} damage to {hit.collider.name}");
                
                // Track punch for quest system
                totalPunches++;
                GameEvents.RaisePunchesChanged(totalPunches);
                Debug.Log($"[PlayerPunching] Total punches: {totalPunches}");
            }
            else
            {
                Debug.LogWarning($"[PlayerPunching] Hit {hit.collider.name} but no IDamageable component found!");
            }
        }
        else
        {
            Debug.Log("[PlayerPunching] Punch missed - no target in range");
        }
    }

    public void SetPunchingEnabled(bool enabled)
    {
        canPunch = enabled;
    }

    // Optional: Visualize punch range in editor
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * punchRange);
        }
    }
}

