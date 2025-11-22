using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EquippedWeaponUI : MonoBehaviour
{
    [Header("Refs")]
    public EquipmentController equipment;   // drag your EquipmentController
    public InventoryDatabase database;      // the DB with icons
    public Image iconImage;                 // the single home-page icon
    public Sprite emptySprite;              // what to show if nothing equipped
    public Color emptyColor = new Color(1,1,1,0.25f);
    public Color filledColor = Color.white;

    void Awake()
    {
        FindEquipmentIfNeeded();
        if (equipment) equipment.OnEquippedChanged += Refresh;
        Refresh(equipment ? equipment.Equipped : null);
    }

    void OnEnable()
    {
        // Re-find equipment if it was lost (e.g., after menu reset)
        FindEquipmentIfNeeded();
        if (equipment) Refresh(equipment.Equipped);
    }

    void FindEquipmentIfNeeded()
    {
        if (!equipment)
        {
            equipment = FindFirstObjectByType<EquipmentController>();
            if (equipment)
            {
                equipment.OnEquippedChanged += Refresh;
            }
        }
    }

    void OnDestroy()
    {
        if (equipment) equipment.OnEquippedChanged -= Refresh;
    }

    void Refresh(string equippedName)
    {
        if (!iconImage) return;

        if (string.IsNullOrEmpty(equippedName) || database == null || !database.TryGet(equippedName, out var entry) || entry.icon == null)
        {
            iconImage.sprite = emptySprite;
            iconImage.color  = emptyColor;
        }
        else
        {
            iconImage.sprite = entry.icon;
            iconImage.color  = filledColor;
        }
    }
}