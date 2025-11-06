// EquipConfirmPopup.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EquipConfirmPopup : MonoBehaviour
{
    public static EquipConfirmPopup SharedInstance { get; private set; }

    public CanvasGroup group; // optional

    string _pendingWeapon;
    EquipmentController _equipment;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        
        // Don't destroy this popup when switching scenes
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

    // called by the grid
    public void ShowFor(string weaponName, EquipmentController equipment)
    {
        _pendingWeapon = weaponName;
        _equipment     = equipment;

        if (group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // Move to end of parent's children list
        
        // Ensure consistent scale
        var rectTransform = transform as RectTransform;
        if (rectTransform)
        {
            rectTransform.localScale = Vector3.one;
        }
        
        // Extra safety: ensure Canvas sorting is still on top
        var canvas = GetComponent<Canvas>();
        if (canvas)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;
            Debug.Log($"[EquipConfirmPopup] Showing '{weaponName}' with sortingOrder={canvas.sortingOrder}, scale={rectTransform?.localScale}");
        }
        else
        {
            Debug.LogWarning($"[EquipConfirmPopup] No Canvas component found! Popup may render behind other UI.");
        }
    }

    // hook these to the prefab's Yes/No buttons - MUST BE PUBLIC
    public bool useMiscSlot;

    public void OnClickYes()
    {
        Debug.Log($"[EquipConfirmPopup] OnClickYes called for '{_pendingWeapon}', useMiscSlot={useMiscSlot}");
        
        if (string.IsNullOrEmpty(_pendingWeapon) || _equipment == null) { Hide(); return; }

        if (useMiscSlot)
            _equipment.EquipMisc(_pendingWeapon);
        else
            _equipment.Equip(_pendingWeapon);

        Hide();
        useMiscSlot = false;
    }

    public void OnClickNo()
    {
        Debug.Log("[EquipConfirmPopup] OnClickNo called");
        Hide();
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
        _pendingWeapon = null;
        _equipment = null;
    }
}