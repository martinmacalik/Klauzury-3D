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

        // Auto-find button if not assigned
        if (!button) button = GetComponent<Button>();
        if (!button) button = GetComponentInChildren<Button>(true);
        if (!button) Debug.LogWarning($"[MiscSlotUI:{name}] Button component not found!");
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

            if (onClicked != null)
            {
                button.onClick.AddListener(() => onClicked.Invoke(_name));
                button.interactable = true;
                Debug.Log($"[MiscSlotUI:{name}] Button enabled for '{itemName}'");
            }
            else
            {
                // No callback means this item shouldn't be clickable (e.g., keycard)
                button.interactable = false;
                button.enabled = false; // Also disable the component itself

                // Also disable any other Button components (in case there are multiple)
                var allButtons = GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                    btn.interactable = false;
                    btn.enabled = false;
                }

                Debug.Log($"[MiscSlotUI:{name}] Button DISABLED for '{itemName}' (disabled {allButtons.Length} button(s))");
            }
        }
        else
        {
            Debug.LogWarning($"[MiscSlotUI:{name}] Button is NULL - cannot disable for '{itemName}'");
        }
    }

    public void SetEquipped(bool isEquipped)
    {
        if (background) background.color = isEquipped ? equippedBg : normalBg;
    }
}