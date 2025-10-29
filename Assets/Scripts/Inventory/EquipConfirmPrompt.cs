// EquipConfirmPopup.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EquipConfirmPopup : MonoBehaviour
{
    public CanvasGroup group; // optional

    string _pendingWeapon;
    EquipmentController _equipment;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        Hide();
    }

    // called by the grid
    public void ShowFor(string weaponName, EquipmentController equipment)
    {
        _pendingWeapon = weaponName;
        _equipment     = equipment;

        if (group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    // hook these to the prefab’s Yes/No buttons
    public void OnClickYes()
    {
        if (_equipment && !string.IsNullOrEmpty(_pendingWeapon))
            _equipment.Equip(_pendingWeapon);
        Hide();
    }

    public void OnClickNo() => Hide();

    void Hide()
    {
        if (group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        _pendingWeapon = null;
        _equipment = null;
    }
}