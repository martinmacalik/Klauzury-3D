// InventoryManager.cs — PNG version
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Database")]
    public InventoryDatabase database;

    [Header("Slots")]
    public IconSlot[] gunSlots;    // e.g., 2 weapon slots
    public IconSlot[] miscSlots;   // general inventory grid

    [Header("Optional: play a sound on add / error")]
    public AudioSource sfx;
    public AudioClip addSfx;
    public AudioClip errorSfx;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!gameObject.scene.IsValid()) return; // ignore prefab stage

        Debug.Log($"[INV] InventoryManager awake on '{name}'. Active={gameObject.activeInHierarchy}, Enabled={enabled}");
    }
    
    public bool TryAddByName(string itemName)
    {
        if (!database || !database.TryGet(itemName, out var e) || !e.icon)
        { Beep(false); return false; }

        var target = e.category == InventoryDatabase.ItemCategory.Gun
            ? FindFirstEmpty(gunSlots)
            : FindFirstEmpty(miscSlots);

        if (!target) { Beep(false); return false; }

        target.SetIcon(e.icon);
        Beep(true);
        return true;
    }

    IconSlot FindFirstEmpty(IconSlot[] arr)
    {
        if (arr == null) return null;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] && arr[i].IsEmpty) return arr[i];
        return null;
    }

    void Beep(bool ok)
    {
        if (!sfx) return;
        sfx.pitch = ok ? 1.05f : 0.8f;
        sfx.PlayOneShot(ok ? addSfx : errorSfx);
    }

    // --- BUY FLOW: charge, place icons, clear basket ---
    // InventoryManager.cs — DIAGNOSTIC version of PurchaseBasket
    public bool PurchaseBasket(Basket basket)
    {
        if (!basket)
        {
            Debug.LogWarning("[INV] PurchaseBasket: basket == null");
            return false;
        }

        Debug.Log(
            $"[INV] PurchaseBasket called. Items={basket.items?.Count ?? 0}, Total=${basket.Total}, canPayHere={basket.canPayHere}");
        if (!basket.canPayHere)
        {
            Debug.LogWarning("[INV] Not at cashier.");
            return false;
        }

        var menu = PlayerMenuController.Instance;
        if (!menu)
        {
            Debug.LogError("[INV] No PlayerMenuController.Instance found.");
            return false;
        }

        int total = basket.Total;
        if (menu.Money < total)
        {
            Debug.LogWarning("[INV] Not enough money.");
            return false;
        }

        if (basket.items == null || basket.items.Count == 0)
        {
            Debug.LogWarning("[INV] Basket empty.");
            return false;
        }

        // Collect names to purchase
        var pending = new List<string>(basket.items.Count);
        foreach (var it in basket.items)
        {
            pending.Add(it.name);
            Debug.Log($"[INV] Pending item: '{it.name}' ${it.price}");
        }

// (Optional) For non-gun items you still want to drop into misc slots:
        var chosenSlots = new List<IconSlot>(pending.Count);
        for (int i = 0; i < pending.Count; i++)
        {
            string name = pending[i];
            if (!database || !database.TryGet(name, out var e) || e == null)
            {
                Debug.LogError($"[INV] DB lookup failed for '{name}'");
                Undo(chosenSlots);
                return false;
            }

            if (!e.icon)
            {
                Debug.LogError($"[INV] DB entry '{name}' has NO icon");
                Undo(chosenSlots);
                return false;
            }

            // NEW: Skip slot placement for guns (they go to Backpack only)
            if (e.category == InventoryDatabase.ItemCategory.Gun)
                continue;

            var slot = FindFirstEmpty(miscSlots);
            if (!slot)
            {
                Debug.LogWarning($"[INV] No empty misc slot for '{name}'.");
                Undo(chosenSlots);
                return false;
            }

            slot.SetIcon(e.icon);
            chosenSlots.Add(slot);
        }

// Push ALL purchased guns into WeaponInventory (Backpack)
        bool boughtWeapon = false;
        var wInv = WeaponInventory.Instance;
        foreach (var name in pending)
        {
            if (database.TryGet(name, out var e) && e.category == InventoryDatabase.ItemCategory.Gun)
            {
                boughtWeapon = true;
                wInv?.AddWeapon(name); // duplicates allowed
            }
        }

        if (boughtWeapon) GameEvents.RaiseWeaponPurchased("any");

// Charge & clear (unchanged)
        Debug.Log($"[INV] Charging ${total} and clearing basket.");
        menu.AddMoney(-total);
        basket.Clear();
        Debug.Log("[INV] Purchase complete.");
        return true;

        void Undo(List<IconSlot> list)
        {
            foreach (var s in list)
                if (s)
                    s.Clear();
        }
    }
}
