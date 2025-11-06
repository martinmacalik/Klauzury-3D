using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class MiscQuestPopup : MonoBehaviour
{
    public static MiscQuestPopup SharedInstance { get; private set; }

    [Header("UI Elements")]
    public CanvasGroup group;
    public TMP_Text messageText; // e.g., "Quest Item Found! Mark as complete?"
    public TMP_Text itemNameText; // Optional: display the item name

    [Header("Quest Items - Add item names that should give stars")]
    public string[] questItemNames = new string[] { "TestBottle" }; // Add your quest items here

    [Header("Integration with StarQuestSystem")]
    public StarQuestSystem questSystem; // Assign your existing quest system

    private string _pendingItem;
    private EquipmentController _equipment;
    private bool _isQuestItem;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        
        if (SharedInstance == null)
        {
            SharedInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (SharedInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Hide();
    }

    void OnDestroy()
    {
        if (SharedInstance == this) SharedInstance = null;
    }

    public void ShowFor(string itemName, EquipmentController equipment)
    {
        _pendingItem = itemName;
        _equipment = equipment;
        _isQuestItem = IsQuestItem(itemName);

        // Auto-find quest system if not assigned
        if (!questSystem) questSystem = FindAnyObjectByType<StarQuestSystem>();

        if (messageText) 
        {
            messageText.text = _isQuestItem 
                ? "Quest Item Found!\nMark quest as complete?" 
                : "Equip this item?";
        }
        if (itemNameText) itemNameText.text = itemName;

        if (group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        
        // Ensure consistent scale
        var rectTransform = transform as RectTransform;
        if (rectTransform) rectTransform.localScale = Vector3.one;
        
        // Ensure Canvas sorting is on top
        var canvas = GetComponent<Canvas>();
        if (canvas)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;
            Debug.Log($"[MiscQuestPopup] Showing '{itemName}' (isQuest={_isQuestItem}) with sortingOrder={canvas.sortingOrder}");
        }
    }

    // Hook to Yes button
    public void OnClickYes()
    {
        Debug.Log($"[MiscQuestPopup] OnClickYes - item '{_pendingItem}', isQuest={_isQuestItem}");
        
        // If it's a quest item, trigger the quest completion through the quest system
        if (_isQuestItem)
        {
            // Fire the quest item found event
            GameEvents.RaiseMiscQuestItemFound(_pendingItem);
            Debug.Log($"[MiscQuestPopup] Fired quest event for '{_pendingItem}'");
        }

        // Equip the item
        if (_equipment != null && !string.IsNullOrEmpty(_pendingItem))
        {
            _equipment.EquipMisc(_pendingItem);
        }

        // Remove the item from inventory (consume it)
        var miscInventory = MiscInventory.Instance;
        if (miscInventory != null && !string.IsNullOrEmpty(_pendingItem))
        {
            bool removed = miscInventory.RemoveOne(_pendingItem);
            Debug.Log($"[MiscQuestPopup] Removed '{_pendingItem}' from inventory: {removed}");
        }
        else
        {
            Debug.LogWarning("[MiscQuestPopup] Could not remove item - MiscInventory.Instance is null!");
        }

        Hide();
    }

    // Hook to No button
    public void OnClickNo()
    {
        Debug.Log("[MiscQuestPopup] OnClickNo - just equipping item without giving star");
        
        // Equip the item
        if (_equipment != null && !string.IsNullOrEmpty(_pendingItem))
        {
            _equipment.EquipMisc(_pendingItem);
        }

        // Remove the item from inventory (consume it)
        var miscInventory = MiscInventory.Instance;
        if (miscInventory != null && !string.IsNullOrEmpty(_pendingItem))
        {
            bool removed = miscInventory.RemoveOne(_pendingItem);
            Debug.Log($"[MiscQuestPopup] Removed '{_pendingItem}' from inventory: {removed}");
        }

        Hide();
    }

    private bool IsQuestItem(string itemName)
    {
        if (questItemNames == null || questItemNames.Length == 0) return false;
        
        foreach (var questName in questItemNames)
        {
            if (string.Equals(questName, itemName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    void Hide()
    {
        if (group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        _pendingItem = null;
        _equipment = null;
        _isQuestItem = false;
    }
}
