using UnityEngine;
using UnityEngine.SceneManagement;

public class HackingBox : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Name of the keycard item required (e.g., 'Keycard')")]
    public string requiredKeycardName = "Keycard";
    
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
    
    private bool _isHighlighted = false;
    
    void Start()
    {
        // Auto-find renderer if not assigned
        if (!boxRenderer) boxRenderer = GetComponent<Renderer>();
        
        // Initialize visual state
        UpdateVisualState(false);
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
        var miscInventory = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
        if (miscInventory == null) return false;
        
        return miscInventory.CountOf(requiredKeycardName) > 0;
    }
    
    public string GetPromptText()
    {
        bool hasKeycard = CanInteract();
        return hasKeycard ? "Press E to hack" : "Keycard required";
    }
    
    public void Interact()
    {
        Debug.Log("[HackingBox] Interact called");
        
        if (!CanInteract())
        {
            Debug.LogWarning($"[HackingBox] Player needs '{requiredKeycardName}' to hack!");
            return;
        }
        
        Debug.Log($"[HackingBox] Keycard found! Loading scene '{hackingSceneName}'");
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

