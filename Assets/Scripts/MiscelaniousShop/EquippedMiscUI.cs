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
        if (equipment) equipment.OnMiscEquippedChanged += Refresh;
        Refresh(equipment ? equipment.EquippedMisc : "");
    }
    void OnDestroy()
    {
        if (equipment) equipment.OnMiscEquippedChanged -= Refresh;
    }

    private void Refresh(string miscName)
    {
        if (string.IsNullOrEmpty(miscName) || !database.TryGet(miscName, out var entry))
        {
            iconImage.sprite = emptySprite;
            iconImage.color = emptyColor;
        }
        else
        {
            iconImage.sprite = entry.icon;
            iconImage.color = equippedColor;
        }
    }
}