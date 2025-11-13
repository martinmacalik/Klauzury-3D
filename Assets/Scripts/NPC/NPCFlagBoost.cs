using UnityEngine;

/// <summary>
/// Detects when the player is near an NPC with a flag enabled and reduces testosterone decay.
/// Attach this to the same NPC GameObject that has the NPCFlag component.
/// </summary>
public class NPCFlagBoost : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float decayMultiplier = 30f; // 1.5 = 50% faster decay
    [SerializeField] private float updateInterval = 0.5f; // Check proximity every 0.5 seconds

    [Header("References")]
    [SerializeField] private NPCFlag npcFlag;
    private Transform playerTransform;
    private TestosteroneSystem testosteroneSystem;
    private float originalDecay;
    private bool isActive = false;
    private float nextUpdateTime = 0f;

    void Awake()
    {
        if (!npcFlag)
            npcFlag = GetComponentInChildren<NPCFlag>();
    }

    void Start()
    {
        // Get testosterone system
        testosteroneSystem = TestosteroneSystem.Instance;
        
        if (!npcFlag)
            npcFlag = GetComponentInChildren<NPCFlag>();
    }

    void Update()
    {
        // Try to find player if we haven't already
        if (!playerTransform)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj)
                playerTransform = playerObj.transform;
            else
                return;
        }

        if (!testosteroneSystem || !npcFlag)
            return;

        // Only check proximity at intervals for performance
        if (Time.time < nextUpdateTime)
            return;

        nextUpdateTime = Time.time + updateInterval;

        // Check if the NPCFlag GameObject itself is active and enabled
        bool flagActive = npcFlag.gameObject.activeInHierarchy && npcFlag.enabled;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = distanceToPlayer <= detectionRadius;

        if (flagActive && inRange && !isActive)
        {
            ApplyBoost();
        }
        else if ((!flagActive || !inRange) && isActive)
        {
            RemoveBoost();
        }
    }

    private void ApplyBoost()
    {
        if (!testosteroneSystem) return;

        isActive = true;
        float currentDecay = 0.5f;
        float boostedDecay = currentDecay * decayMultiplier;
        testosteroneSystem.SetDecay(boostedDecay);
    }

    private void RemoveBoost()
    {
        if (!testosteroneSystem) return;

        isActive = false;
        float baseDecay = 0.5f;
        testosteroneSystem.SetDecay(baseDecay);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection radius in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
