// InventoryDatabase.cs (add the icon field)
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryDatabase", menuName = "Game/Inventory Database")]
public class InventoryDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string name;
        public GameObject prefab;          // keep for future 3D use
        public ItemCategory category = ItemCategory.Generic;
        public Sprite icon;                 // <-- NEW: PNG used in UI
    }

    public enum ItemCategory { Generic, Gun, Keycard }

    public Entry[] items;

    public bool TryGet(string itemName, out Entry entry)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (string.Equals(items[i].name, itemName, System.StringComparison.OrdinalIgnoreCase))
            { entry = items[i]; return true; }
        }
        entry = null;
        return false;
    }
}