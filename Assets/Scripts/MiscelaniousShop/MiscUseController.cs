using UnityEngine;

public class MiscUseController : MonoBehaviour
{
    public EquipmentController equipment;
    public InventoryDatabase database;
    public MiscInventory miscInventory;
    public KeyCode useKey = KeyCode.Q;
    public Transform spawnParent; // where to instantiate item prefab if needed (e.g., under player)

    void Update()
    {
        if (!Input.GetKeyDown(useKey) || equipment == null || miscInventory == null || database == null) return;

        var name = equipment.EquippedMisc;
        if (string.IsNullOrEmpty(name)) return;

        if (!database.TryGet(name, out var entry) || entry.prefab == null)
        {
            Debug.LogWarning($"Equipped misc '{name}' has no prefab/effect.");
            return;
        }

        // Create the effect object (or use pooled version).
        var go = Instantiate(entry.prefab, spawnParent ? spawnParent : transform);
        var consumable = go.GetComponent<IConsumable>();
        bool consumed = false;

        if (consumable != null)
        {
            consumed = consumable.Consume(gameObject);
        }
        else
        {
            // If no IConsumable, assume one-shot effect and consume.
            consumed = true;
        }

        // Destroy the temporary object if it is just an effect holder.
        // (If your prefab is meant to persist, remove this.)
        Destroy(go);

        if (consumed)
        {
            miscInventory.RemoveOne(name);

            // If you want auto-unequip when none left:
            if (miscInventory.CountOf(name) == 0 && string.Equals(equipment.EquippedMisc, name, System.StringComparison.OrdinalIgnoreCase))
                equipment.EquipMisc(""); // clears UI
        }
    }
}