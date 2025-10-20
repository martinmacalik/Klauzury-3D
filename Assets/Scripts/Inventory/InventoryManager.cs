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
    if (!basket) { Debug.LogWarning("[INV] PurchaseBasket: basket == null"); return false; }
    Debug.Log($"[INV] PurchaseBasket called. Items={basket.items?.Count ?? 0}, Total=${basket.Total}, canPayHere={basket.canPayHere}");

    if (!basket.canPayHere) { Debug.LogWarning("[INV] Not at cashier (basket.canPayHere == false)."); return false; } // CashierZone toggles this

    var menu = PlayerMenuController.Instance;
    if (!menu) { Debug.LogError("[INV] No PlayerMenuController.Instance found."); return false; }

    int total = basket.Total;
    Debug.Log($"[INV] Player money={menu.Money}, cost={total}");
    if (menu.Money < total) { Debug.LogWarning("[INV] Not enough money."); return false; }  // 

    if (basket.items == null || basket.items.Count == 0) { Debug.LogWarning("[INV] Basket empty."); return false; }

    // Dry-run + apply
    var pending = new List<string>(basket.items.Count);
    foreach (var it in basket.items)
    {
        Debug.Log($"[INV] Pending item: '{it.name}' ${it.price}");
        pending.Add(it.name);
    }

    var chosenSlots = new List<IconSlot>(pending.Count);

    for (int i = 0; i < pending.Count; i++)
    {
        string name = pending[i];
        if (!database)
        {
            Debug.LogError("[INV] InventoryDatabase is null.");
            Undo(chosenSlots);
            return false;
        }

        if (!database.TryGet(name, out var e) || e == null)
        {
            Debug.LogError($"[INV] DB lookup failed for '{name}'. Check spelling/case in InventoryDatabase.");
            Undo(chosenSlots);
            return false;
        }
        if (!e.icon)
        {
            Debug.LogError($"[INV] DB entry for '{name}' has NO icon sprite assigned.");
            Undo(chosenSlots);
            return false;
        }

        var slot = (e.category == InventoryDatabase.ItemCategory.Gun)
            ? FindFirstEmpty(gunSlots)
            : FindFirstEmpty(miscSlots);

        Debug.Log($"[INV] Item '{name}' → category={e.category}, slotFound={(slot!=null)}");
        if (!slot)
        {
            Debug.LogWarning($"[INV] No empty {(e.category==InventoryDatabase.ItemCategory.Gun ? "gun" : "misc")} slot for '{name}'.");
            Undo(chosenSlots);
            return false;
        }

        slot.SetIcon(e.icon);   // tentative
        Debug.Log($"[INV] SetIcon done on slot '{slot.name}'. Sprite={(e.icon ? e.icon.name : "null")}");
        chosenSlots.Add(slot);
    }

    // Charge & clear
    Debug.Log($"[INV] Charging ${total} and clearing basket...");
    menu.AddMoney(-total);     // clamps >= 0  
    basket.Clear();            // fires onChanged  
    Debug.Log("[INV] Purchase complete.");
    return true;

    void Undo(List<IconSlot> list)
    {
        Debug.Log("[INV] Undo placement for previously filled slots.");
        foreach (var s in list) if (s) s.Clear();
    }
}

}
