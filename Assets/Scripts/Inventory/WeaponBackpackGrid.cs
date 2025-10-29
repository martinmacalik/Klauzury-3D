using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponBackpackGrid : MonoBehaviour
{
    [Header("Refs")]
    public WeaponInventory inventory;
    public InventoryDatabase database;
    public EquipmentController equipment;

    [Header("Grid")]
    public Transform gridRoot;
    public GameObject slotPrefab;     // must have WeaponSlotUI

    [Header("Popup Prefab (with EquipConfirmPopup.cs on it)")]
    public EquipConfirmPopup confirmPrefab;   // DRAG THE PREFAB HERE

    // runtime
    EquipConfirmPopup _confirmInstance;
    readonly List<GameObject> _spawned = new();
    readonly List<WeaponSlotUI> _slots = new();

    void Awake()
    {
        if (!inventory) inventory = WeaponInventory.Instance;
        if (!equipment) equipment = FindObjectOfType<EquipmentController>(true);
        if (equipment) equipment.OnEquippedChanged += OnEquippedChanged;
        Rebuild();
    }

    void OnEnable()
    {
        if (!inventory) inventory = WeaponInventory.Instance;
        if (inventory) inventory.OnChanged += Rebuild;
        if (equipment) equipment.OnEquippedChanged += OnEquippedChanged;
        Rebuild();
    }

    void OnDisable()
    {
        if (inventory) inventory.OnChanged -= Rebuild;
        if (equipment) equipment.OnEquippedChanged -= OnEquippedChanged;
    }
    
    void OnDestroy()
    {
        if (equipment) equipment.OnEquippedChanged -= OnEquippedChanged;  // NEW
    }

    public void Rebuild()
    {
        if (!gridRoot || !slotPrefab || inventory == null || database == null) return;

        for (int i = 0; i < _spawned.Count; i++) if (_spawned[i]) Destroy(_spawned[i]);
        _spawned.Clear();
        _slots.Clear();

        var list = inventory.Owned;
        for (int i = 0; i < list.Count; i++)
        {
            string name = list[i];
            Sprite icon = database.TryGet(name, out var entry) ? entry.icon : null;

            var go = Instantiate(slotPrefab, gridRoot);
            _spawned.Add(go);

            var slot = go.GetComponent<WeaponSlotUI>();
            if (!slot) { Debug.LogError("Slot prefab missing WeaponSlotUI."); continue; }

            slot.Setup(name, icon, OnSlotClicked);
            _slots.Add(slot);
        }

        RefreshEquippedHighlight(); // NEW
    }
    
    void OnEquippedChanged(string _)
    {
        RefreshEquippedHighlight(); // NEW
    }
    
    void RefreshEquippedHighlight() 
    {
        string equipped = equipment ? equipment.Equipped : null; for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (!s) continue;
            s.SetEquipped(!string.IsNullOrEmpty(equipped) && s.weaponName == equipped);
        }
    }

    void OnSlotClicked(WeaponSlotUI slot)
    {
        if (!slot) return;
        var popup = GetConfirm();
        if (popup == null) return;

        popup.ShowFor(slot.weaponName, equipment); // popup handles Yes/No + equip
    }

    EquipConfirmPopup GetConfirm()
{
    if (_confirmInstance) return _confirmInstance;

    if (!confirmPrefab)
    {
        Debug.LogError("[Backpack] confirmPrefab not assigned.");
        return null;
    }

    // 1) Always parent under a top-level Overlay canvas (outside any masks)
    var overlay = EnsureOverlayCanvas(); // creates one if missing (see method below)

    // 2) Instantiate under that overlay
    _confirmInstance = Instantiate(confirmPrefab, overlay.transform);
    _confirmInstance.name = "EquipConfirmPopup (Instance)";

    // 3) Give the popup its own Canvas that sorts ABOVE everything
    var selfCanvas = _confirmInstance.GetComponent<Canvas>();
    if (!selfCanvas) selfCanvas = _confirmInstance.gameObject.AddComponent<Canvas>();
    selfCanvas.overrideSorting = true;
    selfCanvas.sortingOrder = overlay.sortingOrder + 100; // on top of overlay

    // Important for input:
    if (!_confirmInstance.GetComponent<GraphicRaycaster>())
        _confirmInstance.gameObject.AddComponent<GraphicRaycaster>();

    // Bring to top inside overlay
    _confirmInstance.transform.SetAsLastSibling();

    return _confirmInstance;
}

// Same helper you can keep in this class:
static Canvas EnsureOverlayCanvas()
{
    // Try to find an existing top-level ScreenSpaceOverlay canvas
    var canvases = Object.FindObjectsOfType<Canvas>();
    for (int i = 0; i < canvases.Length; i++)
    {
        var c = canvases[i];
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
    canvas.sortingOrder = 5000; // very high

    var scaler = go.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);
    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    scaler.matchWidthOrHeight = 0.5f;

    return canvas;
}

}
