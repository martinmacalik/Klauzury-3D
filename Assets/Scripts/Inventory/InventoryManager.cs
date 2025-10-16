using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Database")]
    public InventoryDatabase database;

    [Header("Slots")]
    public Mini3DSlot[] gunSlots;    // dedicated gun slots (e.g., 2)
    public Mini3DSlot[] miscSlots;   // generic inventory grid

    [Header("Optional: play a sound on add / error")]
    public AudioSource sfx;
    public AudioClip addSfx;
    public AudioClip errorSfx;

    public bool TryAddByName(string itemName)
    {
        if (!database || !database.TryGet(itemName, out var e) || !e.prefab)
        { Beep(false); return false; }

        Mini3DSlot target = null;
        if (e.category == InventoryDatabase.ItemCategory.Gun)
            target = FindFirstEmpty(gunSlots);
        else
            target = FindFirstEmpty(miscSlots);

        if (!target) { Beep(false); return false; }

        target.ShowPrefab(e.prefab);
        Beep(true);
        return true;
    }

    Mini3DSlot FindFirstEmpty(Mini3DSlot[] arr)
    {
        if (arr == null) return null;
        for (int i = 0; i < arr.Length; i++)
        {
            // crude emptiness check: RawImage has a texture but we track via a private flag instead; Mini3DSlot.Clear destroys model.
            // We'll add a tiny helper: consider "empty" if there's no child under the slot's Pivot.
            var childCount = arr[i].transform.Find("Pivot")?.childCount ?? -1;
            if (childCount <= 0) return arr[i];
        }
        return null;
    }

    void Beep(bool ok)
    {
        if (!sfx) return;
        sfx.pitch = ok ? 1.05f : 0.8f;
        sfx.PlayOneShot(ok ? addSfx : errorSfx);
    }

    // --- BUY FLOW: consume basket, subtract money, fill slots ---
    public bool PurchaseBasket(Basket basket)
    {
        if (!basket) return false;
        if (!basket.canPayHere) { Debug.Log("Not at cashier."); return false; } // guarded by CashierZone trigger
        var menu = PlayerMenuController.Instance;
        if (!menu) { Debug.LogWarning("No PlayerMenuController."); return false; }

        int total = basket.Total;                                    // sum of items in basket
        if (menu.Money < total) return false;                        // not enough money  :contentReference[oaicite:1]{index=1}

        // Try placing ALL items first. If any fail (no space), abort.
        var pending = new List<string>(basket.items.Count);
        foreach (var it in basket.items) pending.Add(it.name);

        // dry-run to check space
        var filled = new List<Mini3DSlot>();
        foreach (var name in pending)
        {
            if (!database || !database.TryGet(name, out var e) || !e.prefab) { Undo(filled); return false; }
            var slot = e.category == InventoryDatabase.ItemCategory.Gun ? FindFirstEmpty(gunSlots)
                                                                        : FindFirstEmpty(miscSlots);
            if (!slot) { Undo(filled); return false; }
            slot.ShowPrefab(e.prefab);
            filled.Add(slot);
        }

        // charge money, clear basket, done
        menu.AddMoney(-total);                                       // subtracts & clamps >= 0  :contentReference[oaicite:2]{index=2}
        basket.Clear();                                              // empties basket & triggers onChanged  :contentReference[oaicite:3]{index=3}
        return true;

        void Undo(List<Mini3DSlot> addList)
        {
            foreach (var s in addList) s.Clear();
        }
    }
}
