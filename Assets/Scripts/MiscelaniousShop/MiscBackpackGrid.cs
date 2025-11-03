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
    public EquipConfirmPopup confirmPrefab; // reuse your existing popup

    private readonly List<MiscSlotUI> _slots = new();
    private EquipConfirmPopup _confirm;

    void OnEnable()
    {
        if (miscInventory) miscInventory.OnChanged += Rebuild;
        if (equipment) equipment.OnMiscEquippedChanged += RefreshEquipped;
        Rebuild();
    }
    void OnDisable()
    {
        if (miscInventory) miscInventory.OnChanged -= Rebuild;
        if (equipment) equipment.OnMiscEquippedChanged -= RefreshEquipped;
    }

    public void Rebuild()
    {
        if (!gridRoot || !slotPrefab || miscInventory == null || database == null) return;

        foreach (Transform c in gridRoot) Destroy(c.gameObject);
        _slots.Clear();

        foreach (var name in miscInventory.Owned)
        {
            if (!database.TryGet(name, out var entry)) continue;
            var slot = Instantiate(slotPrefab, gridRoot);
            slot.Setup(name, entry.icon, OnSlotClicked);
            _slots.Add(slot);
        }
        RefreshEquipped(equipment ? equipment.EquippedMisc : "");
    }

    private void OnSlotClicked(string itemName)
    {
        if (_confirm == null)
        {
            _confirm = Instantiate(confirmPrefab);
            EnsureOverlayCanvas(_confirm.transform);
        }
        _confirm.useMiscSlot = true;             // <-- add this line (see popup change below)
        _confirm.ShowFor(itemName, equipment);   // pass the EquipmentController, not a method
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

    private void EnsureOverlayCanvas(Transform t)
    {
        var canvas = FindAnyObjectByType<Canvas>();
        Canvas overlay = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { overlay = c; break; }
        if (!overlay)
        {
            var go = new GameObject("OverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay = go.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        t.SetParent(overlay.transform, false);
    }
}
