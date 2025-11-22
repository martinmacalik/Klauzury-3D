using UnityEngine;
using UnityEngine.UI;

public class EquippedMiscUI : MonoBehaviour
{
    public EquipmentController equipment;
    public InventoryDatabase database;
    public Image iconImage;
    public Sprite emptySprite;
    public Color emptyColor = new Color(1,1,1,0.25f);
    public Color equippedColor = Color.white;

    void Awake()
    {
        FindEquipmentIfNeeded();
        if (equipment) equipment.OnMiscEquippedChanged += Refresh;

        // VALIDATION: Check if our iconImage is part of an IconSlot
        if (iconImage)
        {
            var parentIconSlot = iconImage.GetComponentInParent<IconSlot>();
            if (parentIconSlot != null)
            {
                Debug.LogError($"[EquippedMiscUI:{gameObject.name}] ⚠️ CONFIGURATION ERROR! This EquippedMiscUI's iconImage is inside an IconSlot ('{parentIconSlot.name}'). This will cause the keycard to appear in misc slots when equipped! FIX: Create a separate Image for EquippedMiscUI, don't use an IconSlot's image.");
            }
        }

        Refresh(equipment ? equipment.EquippedMisc : "");
    }

    void OnEnable()
    {
        // Re-find equipment if it was lost (e.g., after menu reset)
        FindEquipmentIfNeeded();
        if (equipment) Refresh(equipment.EquippedMisc);
    }

    void FindEquipmentIfNeeded()
    {
        if (!equipment)
        {
            equipment = FindFirstObjectByType<EquipmentController>();
            if (equipment)
            {
                equipment.OnMiscEquippedChanged += Refresh;
            }
        }
    }
    void OnDestroy()
    {
        if (equipment) equipment.OnMiscEquippedChanged -= Refresh;
    }

    private void Refresh(string miscName)
    {
        // Safety check - ensure iconImage is assigned
        if (!iconImage)
        {
            Debug.LogError("[EquippedMiscUI] iconImage is not assigned in the Inspector!");
            return;
        }

        Debug.Log($"[EquippedMiscUI:{gameObject.name}] Refresh called for '{miscName}', iconImage.gameObject='{iconImage.gameObject.name}'");

        if (string.IsNullOrEmpty(miscName) || !database || !database.TryGet(miscName, out var entry))
        {
            iconImage.sprite = emptySprite;
            iconImage.color = emptyColor;
            Debug.Log($"[EquippedMiscUI:{gameObject.name}] Set to empty");
        }
        else
        {
            iconImage.sprite = entry.icon;
            iconImage.color = equippedColor;
            Debug.Log($"[EquippedMiscUI:{gameObject.name}] Set icon to '{entry.icon.name}' on Image '{iconImage.gameObject.name}'");
        }
    }
}