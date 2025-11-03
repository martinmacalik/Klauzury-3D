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

    public void Setup(string itemName, Sprite icon, Action<string> onClicked)
    {
        _name = itemName;
        if (iconSlot) iconSlot.SetIcon(icon);
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