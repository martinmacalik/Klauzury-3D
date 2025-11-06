using System;
using UnityEngine;
using UnityEngine.UI;

public class MiscSlotUI : MonoBehaviour
{
    [Header("Refs")]
    public Image background;
    public IconSlot iconSlot; // reuse your IconSlot for consistent sizing
    public Button button;

    [Header("Colors")]
    public Color normalBg = new Color(0,0,0,0.25f);
    public Color equippedBg = new Color(0.1f,0.6f,0.2f,0.5f);

    private string _name;

    void Awake()
    {
        // Auto-find iconSlot if not assigned
        if (!iconSlot) iconSlot = GetComponentInChildren<IconSlot>(true);
        if (!iconSlot) Debug.LogError($"[MiscSlotUI:{name}] IconSlot component not found!");
    }

    public void Setup(string itemName, Sprite icon, Action<string> onClicked)
    {
        _name = itemName;
        
        Debug.Log($"[MiscSlotUI:{name}] Setup called: itemName='{itemName}', icon={(icon ? icon.name : "NULL")}, iconSlot={(iconSlot ? "assigned" : "NULL")}");
        
        if (iconSlot)
        {
            iconSlot.SetIcon(icon);
            Debug.Log($"[MiscSlotUI:{name}] After SetIcon, iconSlot.IsEmpty={iconSlot.IsEmpty}");
        }
        else
        {
            Debug.LogError($"[MiscSlotUI:{name}] iconSlot is NULL - cannot set icon!");
        }
        
        if (background) background.color = normalBg;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(_name));
        }
    }

    public void SetEquipped(bool isEquipped)
    {
        if (background) background.color = isEquipped ? equippedBg : normalBg;
    }
}