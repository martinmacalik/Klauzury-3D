using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EquipmentController : MonoBehaviour
{
    public event Action<string> OnEquippedChanged;

    [SerializeField] private string equippedWeaponName;

    public string Equipped => equippedWeaponName;

    public void Equip(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return;
        if (equippedWeaponName == weaponName) return;
        equippedWeaponName = weaponName;
        OnEquippedChanged?.Invoke(equippedWeaponName);
        // Debug.Log($"[Equip] Equipped '{equippedWeaponName}'");
    }
}