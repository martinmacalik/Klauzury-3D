using UnityEngine;
using UnityEngine.SceneManagement;

public class HackingBox : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Name of the keycard item required (e.g., 'Keycard')")]
    public string requiredKeycardName = "Keycard";
    [Tooltip("Minimum stars required to hack (checks StarQuestSystem)")]
    public int minStarsRequired = 3;
    [Tooltip("Minimum testosterone percentage required (0-100)")]
    public float minTestosteronePercent = 90f;
    
    [Header("Debug")]
    [Tooltip("Bypass all requirements for testing")]
    public bool debugBypassRequirements;
    [Tooltip("Set stars for testing (requires StarQuestSystem)")]
    public int debugSetStars;
    
    [ContextMenu("Enable Debug Bypass")]
    void EnableDebugBypass()
    {
        debugBypassRequirements = true;
        Debug.Log("[HackingBox] Debug bypass ENABLED - all requirements bypassed");
    }
    
    [ContextMenu("Disable Debug Bypass")]
    void DisableDebugBypass()
    {
        debugBypassRequirements = false;
        Debug.Log("[HackingBox] Debug bypass DISABLED - normal requirements apply");
    }
    
    [ContextMenu("Apply Debug Stars")]
    void ApplyDebugStars()
    {
        var starSystem = FindFirstObjectByType<StarQuestSystem>();
        if (starSystem == null)
        {
            Debug.LogWarning("[HackingBox] StarQuestSystem not found in scene!");
            return;
        }
        
        if (debugSetStars <= 0)
        {
            Debug.LogWarning("[HackingBox] debugSetStars must be greater than 0");
            return;
        }
        
        // Complete quests to reach the desired star count
        int starsSet = 0;
        for (int i = 0; i < debugSetStars && i < starSystem.quests.Count; i++)
        {
            starSystem.quests[i].completed = true;
            starsSet++;
        }
        
        // Force refresh
        var refreshMethod = starSystem.GetType().GetMethod("RefreshStars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (refreshMethod != null)
        {
            refreshMethod.Invoke(starSystem, null);
            Debug.Log($"[HackingBox] Debug: Successfully set {starsSet} stars and refreshed display");
        }
        else
        {
            Debug.LogWarning("[HackingBox] Could not find RefreshStars method, but stars were set");
            Debug.Log($"[HackingBox] Debug: Set {starsSet} stars (display may not update)");
        }
    }
    
    [Header("Return Spawn Location")]
    [Tooltip("Position where player will spawn when returning from hack (set in HackingSceneManager)")]
    public Transform playerSpawnPoint;
    
    [ContextMenu("Set Spawn From This Position")]
    void SetSpawnFromThisPosition()
    {
        var manager = HackingSceneManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[HackingBox] HackingSceneManager not found! Make sure it's on your GameController.");
            return;
        }
        
        Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : transform.position + transform.forward * 2f;
        Vector3 spawnRot = playerSpawnPoint != null ? playerSpawnPoint.eulerAngles : transform.eulerAngles;
        
        // Use reflection to set the values since they're public fields
        var posField = manager.GetType().GetField("returnSpawnPosition");
        var rotField = manager.GetType().GetField("returnSpawnRotation");
        
        if (posField != null && rotField != null)
        {
            posField.SetValue(manager, spawnPos);
            rotField.SetValue(manager, spawnRot);
            Debug.Log($"[HackingBox] Set spawn position to {spawnPos} with rotation {spawnRot}");
        }
    }
    
    [Header("Scene to Load")]
    [Tooltip("Name of the hacking scene to load")]
    public string hackingSceneName = "HackingScene";
    
    [Header("Visual Feedback")]
    public Renderer boxRenderer;
    public Material normalMaterial;
    public Material highlightMaterial;
    
    [Header("Optional")]
    public GameObject lockedEffect; // Visual effect when locked (no keycard)
    public GameObject unlockedEffect; // Visual effect when unlocked (has keycard)
    public TMPro.TextMeshProUGUI requirementsText; // Display missing requirements
    
    private bool _isHighlighted;
    
    private bool _isHacked;
    
    void Start()
    {
        // Auto-find renderer if not assigned
        if (!boxRenderer) boxRenderer = GetComponent<Renderer>();
        
        // Initialize visual state
        UpdateVisualState(false);
        
        // Hide requirements text initially and clear any default text
        if (requirementsText != null)
        {
            requirementsText.text = "";
            requirementsText.gameObject.SetActive(false);
        }
        
        // Check if player just returned from a successful hack
        CheckHackResult();
    }
    
    void CheckHackResult()
    {
        if (HackingSceneManager.WasLastHackSuccessful())
        {
            Debug.Log("[HackingBox] Player successfully completed the hack!");
            _isHacked = true;
            
            // Reposition player if needed
            if (HackingSceneManager.ShouldRepositionPlayer)
            {
                RepositionPlayer();
            }
            
            HackingSceneManager.ResetHackStatus(); // Clear the flag
            
            // You can add rewards here, trigger events, etc.
            // Example: GameEvents.OnHackingBoxCompleted?.Invoke();
        }
    }
    
    void RepositionPlayer()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[HackingBox] Player not found with tag 'Player' - trying PlayerRoot");
            player = GameObject.Find("PlayerRoot");
        }
        
        if (player != null)
        {
            // Set position and rotation
            player.transform.position = HackingSceneManager.SpawnPosition;
            player.transform.eulerAngles = HackingSceneManager.SpawnRotation;
            
            Debug.Log($"[HackingBox] Player repositioned to {HackingSceneManager.SpawnPosition} with rotation {HackingSceneManager.SpawnRotation}");
        }
        else
        {
            Debug.LogWarning("[HackingBox] Could not find player to reposition!");
        }
    }
    
    public bool IsHacked()
    {
        return _isHacked;
    }
    
    public void SetHighlighted(bool highlighted)
    {
        if (_isHighlighted == highlighted) return;
        _isHighlighted = highlighted;
        
        UpdateVisualState(highlighted);
    }
    
    void UpdateVisualState(bool highlighted)
    {
        // Change material for highlight
        if (boxRenderer && normalMaterial && highlightMaterial)
        {
            boxRenderer.material = highlighted ? highlightMaterial : normalMaterial;
        }
        
        // Update visual effects based on keycard availability
        var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
        bool hasKeycard = miscInventory != null && miscInventory.CountOf(requiredKeycardName) > 0;
        
        if (highlighted)
        {
            if (lockedEffect) lockedEffect.SetActive(!hasKeycard);
            if (unlockedEffect) unlockedEffect.SetActive(hasKeycard);
        }
        else
        {
            if (lockedEffect) lockedEffect.SetActive(false);
            if (unlockedEffect) unlockedEffect.SetActive(false);
        }
    }
    
    public bool CanInteract()
    {
        // Debug bypass
        if (debugBypassRequirements) return true;
        
        // Check keycard
        var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
        if (miscInventory == null) return false;
        
        if (miscInventory.CountOf(requiredKeycardName) <= 0)
            return false;
        
        // Check testosterone
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            float currentPercent = testSystem.Normalized * 100f;
            if (currentPercent < minTestosteronePercent)
                return false;
        }
        
        // Check stars
        var starSystem = FindFirstObjectByType<StarQuestSystem>();
        if (starSystem != null)
        {
            int currentStars = 0;
            foreach (var quest in starSystem.quests)
            {
                if (quest.completed) currentStars++;
            }
            
            if (currentStars < minStarsRequired)
                return false;
        }
        
        return true;
    }
    
    public string GetPromptText()
    {
        // Check if already hacked
        if (_isHacked)
            return "Already hacked";
        
        // Debug bypass
        if (debugBypassRequirements)
            return "Press E to hack [DEBUG]";
        
        // Check keycard first
        var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
        if (miscInventory == null || miscInventory.CountOf(requiredKeycardName) <= 0)
            return "Keycard required";
        
        // Check testosterone
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            float currentPercent = testSystem.Normalized * 100f;
            if (currentPercent < minTestosteronePercent)
                return $"Need {minTestosteronePercent}% testosterone";
        }
        
        // Check stars
        var starSystem = FindFirstObjectByType<StarQuestSystem>();
        if (starSystem != null)
        {
            int currentStars = 0;
            foreach (var quest in starSystem.quests)
            {
                if (quest.completed) currentStars++;
            }
            
            if (currentStars < minStarsRequired)
                return $"Need {minStarsRequired} stars";
        }
        
        return "Press E to hack";
    }
    
    string GetMissingRequirementsList()
    {
        if (debugBypassRequirements)
        {
            Debug.Log("[HackingBox] Debug bypass enabled - all requirements bypassed");
            return "All requirements bypassed [DEBUG MODE]";
        }
        
        var missing = new System.Collections.Generic.List<string>();
        
        // Check keycard
        var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
        int keycardCount = miscInventory?.CountOf(requiredKeycardName) ?? 0;
        Debug.Log($"[HackingBox] Keycard check: have {keycardCount}, need 1");
        if (miscInventory == null || keycardCount <= 0)
        {
            missing.Add($"You need to have a {requiredKeycardName}");
        }
        
        // Check testosterone
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            float currentPercent = testSystem.Normalized * 100f;
            Debug.Log($"[HackingBox] Testosterone check: have {currentPercent:F1}%, need {minTestosteronePercent}%");
            if (currentPercent < minTestosteronePercent)
            {
                missing.Add($"You need to have at least {minTestosteronePercent}% of testosterone");
            }
        }
        
        // Check stars
        var starSystem = FindFirstObjectByType<StarQuestSystem>();
        if (starSystem != null)
        {
            int currentStars = 0;
            foreach (var quest in starSystem.quests)
            {
                if (quest.completed) currentStars++;
            }
            
            Debug.Log($"[HackingBox] Stars check: have {currentStars}, need {minStarsRequired}");
            
            if (currentStars < minStarsRequired)
            {
                missing.Add($"You need to have at least {minStarsRequired} stars of testosterone level");
            }
        }
        else
        {
            Debug.LogWarning("[HackingBox] StarQuestSystem not found in scene!");
        }
        
        // If all requirements are met, return success message
        if (missing.Count == 0)
        {
            Debug.Log("[HackingBox] All requirements met!");
            return "All requirements met! Press E to hack.";
        }
        
        Debug.Log($"[HackingBox] Missing {missing.Count} requirement(s)");
        return string.Join("\n\n", missing);
    }
    
    void ShowMissingRequirements()
    {
        if (requirementsText == null)
        {
            Debug.LogWarning("[HackingBox] Requirements text is not assigned!");
            return;
        }
        
        string missingList = GetMissingRequirementsList();
        Debug.Log($"[HackingBox] Displaying requirements: {missingList}");
        requirementsText.text = missingList;
        requirementsText.gameObject.SetActive(true);
        
        // Auto-hide after 3 seconds
        CancelInvoke(nameof(HideRequirementsText));
        Invoke(nameof(HideRequirementsText), 3f);
    }
    
    void HideRequirementsText()
    {
        if (requirementsText != null)
            requirementsText.gameObject.SetActive(false);
    }
    
    public void Interact()
    {
        Debug.Log("[HackingBox] Interact called");
        
        // Prevent interaction if already hacked
        if (_isHacked)
        {
            Debug.Log("[HackingBox] This box has already been hacked!");
            return;
        }
        
        if (!CanInteract())
        {
            // Show missing requirements in TMP
            ShowMissingRequirements();
            
            // Detailed failure message for console
            var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
            if (miscInventory == null || miscInventory.CountOf(requiredKeycardName) <= 0)
            {
                Debug.LogWarning($"[HackingBox] Missing requirement: '{requiredKeycardName}'");
                return;
            }
            
            var testSystem = TestosteroneSystem.Instance;
            if (testSystem != null)
            {
                float currentPercent = testSystem.Normalized * 100f;
                if (currentPercent < minTestosteronePercent)
                {
                    Debug.LogWarning($"[HackingBox] Missing requirement: Testosterone at {currentPercent:F1}%, need {minTestosteronePercent}%");
                    return;
                }
            }
            
            var starSystem = FindFirstObjectByType<StarQuestSystem>();
            if (starSystem != null)
            {
                int currentStars = 0;
                foreach (var quest in starSystem.quests)
                {
                    if (quest.completed) currentStars++;
                }
                
                if (currentStars < minStarsRequired)
                {
                    Debug.LogWarning($"[HackingBox] Missing requirement: {currentStars} stars, need {minStarsRequired}");
                    return;
                }
            }
            
            Debug.LogWarning("[HackingBox] Requirements not met!");
            return;
        }
        
        Debug.Log($"[HackingBox] All requirements met! Loading scene '{hackingSceneName}'");
        LoadHackingScene();
    }
    
    void LoadHackingScene()
    {
        if (string.IsNullOrEmpty(hackingSceneName))
        {
            Debug.LogError("[HackingBox] Hacking scene name is not set!");
            return;
        }
        
        // Check if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(hackingSceneName))
        {
            SceneManager.LoadScene(hackingSceneName);
        }
        else
        {
            Debug.LogError($"[HackingBox] Scene '{hackingSceneName}' not found in Build Settings! Add it to File > Build Settings > Scenes In Build");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw a wire box to show interaction area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale * 1.2f);
    }
}

