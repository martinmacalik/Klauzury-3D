using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiscBackpackGrid : MonoBehaviour
{
    [Header("Data")]
    public MiscInventory miscInventory;
    public InventoryDatabase database;
    public EquipmentController equipment;

    [Header("UI")]
    public Transform gridRoot;
    public MiscSlotUI slotPrefab;
    public EquipConfirmPopup confirmPrefab; // for regular misc items
    public MiscQuestPopup questPopupPrefab; // for quest items

    [Header("Items that skip popup (equip directly)")]
    public string[] noPopupItems = new string[] { "Keycard" }; // Add items that should equip without popup

    private readonly List<MiscSlotUI> _slots = new();
    private EquipConfirmPopup _confirm;
    private MiscQuestPopup _questPopup;

    void OnEnable()
    {
        FindEquipmentIfNeeded();
        if (miscInventory) miscInventory.OnChanged += Rebuild;
        if (equipment) equipment.OnMiscEquippedChanged += RefreshEquipped;
        Rebuild();
    }
    void OnDisable()
    {
        if (miscInventory) miscInventory.OnChanged -= Rebuild;
        if (equipment) equipment.OnMiscEquippedChanged -= RefreshEquipped;
    }

    void FindEquipmentIfNeeded()
    {
        if (!equipment)
        {
            equipment = FindFirstObjectByType<EquipmentController>();
            if (equipment)
            {
                equipment.OnMiscEquippedChanged += RefreshEquipped;
            }
        }
    }

    public void Rebuild()
    {
        if (!gridRoot || !slotPrefab || miscInventory == null || database == null) return;

        foreach (Transform c in gridRoot) Destroy(c.gameObject);
        _slots.Clear();

        Debug.Log($"[MiscBackpackGrid] Rebuilding with {miscInventory.Owned.Count} items");

        foreach (var name in miscInventory.Owned)
        {
            if (!database.TryGet(name, out var entry))
            {
                Debug.LogWarning($"[MiscBackpackGrid] Item '{name}' not found in database!");
                continue;
            }
            
            Debug.Log($"[MiscBackpackGrid] Creating slot for '{name}', icon={(entry.icon ? entry.icon.name : "NULL")}");
            
            var slot = Instantiate(slotPrefab, gridRoot);

            // If this item is in the noPopupItems list, disable button interaction
            if (ShouldSkipPopup(name))
            {
                slot.Setup(name, entry.icon, null); // No callback = not clickable
                Debug.Log($"[MiscBackpackGrid] '{name}' is in no-popup list - making it non-clickable");
            }
            else
            {
                slot.Setup(name, entry.icon, OnSlotClicked);
            }

            _slots.Add(slot);
        }
        RefreshEquipped(equipment ? equipment.EquippedMisc : "");
    }

    private void OnSlotClicked(string itemName)
    {
        // Note: Items in noPopupItems list (like keycard) won't trigger this because they're non-clickable

        // Use the MiscQuestPopup for all clickable items (handles both quest and non-quest items)
        if (_questPopup == null)
        {
            if (MiscQuestPopup.SharedInstance != null)
            {
                _questPopup = MiscQuestPopup.SharedInstance;
            }
            else if (questPopupPrefab != null)
            {
                var overlay = EnsureOverlayCanvas();
                _questPopup = Instantiate(questPopupPrefab, overlay.transform);
                _questPopup.name = "MiscQuestPopup (Shared)";

                // Add Canvas component with high sorting order
                var selfCanvas = _questPopup.GetComponent<Canvas>();
                if (!selfCanvas) selfCanvas = _questPopup.gameObject.AddComponent<Canvas>();
                selfCanvas.overrideSorting = true;
                selfCanvas.sortingOrder = 9999;

                if (!_questPopup.GetComponent<GraphicRaycaster>())
                    _questPopup.gameObject.AddComponent<GraphicRaycaster>();

                // Ensure scale is 1
                var rt = _questPopup.transform as RectTransform;
                if (rt) rt.localScale = Vector3.one;

                _questPopup.transform.SetAsLastSibling();

                Debug.Log($"[MiscBackpackGrid] Created quest popup with sortingOrder={selfCanvas.sortingOrder}");
            }
            else
            {
                Debug.LogError("[MiscBackpackGrid] questPopupPrefab not assigned!");
                return;
            }
        }

        _questPopup.ShowFor(itemName, equipment);
    }
    
    private void OnConfirmEquip(string itemName)
    {
        equipment.EquipMisc(itemName);
        RefreshEquipped(itemName);
    }

    private void RefreshEquipped(string equippedName)
    {
        foreach (var s in _slots)
            s.SetEquipped(!string.IsNullOrEmpty(equippedName) &&
                string.Equals(GetSlotName(s), equippedName, System.StringComparison.OrdinalIgnoreCase));
    }

    private string GetSlotName(MiscSlotUI slot) => slot ? slot.name.Replace("(Clone)", "").Trim() : "";

    private bool ShouldSkipPopup(string itemName)
    {
        if (noPopupItems == null || noPopupItems.Length == 0) return false;

        foreach (var noPopupItem in noPopupItems)
        {
            if (string.Equals(noPopupItem, itemName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private Canvas EnsureOverlayCanvas()
    {
        // Try to find an existing top-level ScreenSpaceOverlay canvas
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (!c) continue;
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        }

        // Create one if none
        var go = new GameObject("UI Overlay Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }
}
