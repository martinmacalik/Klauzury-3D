using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EquipmentController : MonoBehaviour
{
    public event Action<string> OnEquippedChanged;
    [SerializeField] private string equippedWeaponName;
    public string Equipped => equippedWeaponName;
    
    public string EquippedMisc => equippedMiscName;
    [SerializeField] private string equippedMiscName = "";
    public event System.Action<string> OnMiscEquippedChanged;

    public void Equip(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return;
        if (equippedWeaponName == weaponName) return;
        equippedWeaponName = weaponName;
        OnEquippedChanged?.Invoke(equippedWeaponName);
        // Debug.Log($"[Equip] Equipped '{equippedWeaponName}'");
    }
    
    public void EquipMisc(string miscName)
    {
        if (string.IsNullOrWhiteSpace(miscName)) return;
        if (string.Equals(equippedMiscName, miscName, StringComparison.OrdinalIgnoreCase)) return;
        equippedMiscName = miscName;
        OnMiscEquippedChanged?.Invoke(equippedMiscName);
    }
}