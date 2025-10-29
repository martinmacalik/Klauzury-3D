// WeaponSlotUI.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WeaponSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;                    // optional: direct Image fallback
    public Button button;

    [Header("Background (equipped highlight)")]
    public Image background;              // <- ASSIGN your slot's bg image here
    public Color normalBg   = Color.white;
    public Color equippedBg = new Color(0.2f, 1f, 0.2f, 0.35f); // soft green

    [Header("Optional: IconSlot (preferred)")]
    public IconSlot iconSlot;             // <- if present, uses exact Home sizing

    [HideInInspector] public string weaponName;

    System.Action<WeaponSlotUI> _onClicked;

    void Awake()
    {
        if (!icon)   icon   = GetComponentInChildren<Image>(true);
        if (!button) button = GetComponentInChildren<Button>(true);
        if (!iconSlot) iconSlot = GetComponent<IconSlot>();   // auto-grab if present
    }

    public void Setup(string name, Sprite sprite, System.Action<WeaponSlotUI> onClicked)
    {
        weaponName = name;
        _onClicked = onClicked;

        // Prefer IconSlot so size matches Home
        if (iconSlot)
        {
            iconSlot.SetIcon(sprite);
        }
        else if (icon)
        {
            icon.sprite = sprite;
            icon.color  = sprite ? Color.white : new Color(1,1,1,0.25f);
            icon.preserveAspect = true;
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClicked?.Invoke(this));
        }

        SetEquipped(false);
    }

    public void SetEquipped(bool isEquipped)
    {
        if (background) background.color = isEquipped ? equippedBg : normalBg;
    }
}