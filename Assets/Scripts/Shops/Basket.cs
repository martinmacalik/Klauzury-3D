using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Basket : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public string name;
        public int price;
        public Item(string n, int p) { name = n; price = p; }
    }

    public List<Item> items = new List<Item>();
    public UnityEvent onChanged;
    public UnityEvent onPaid;

    [Tooltip("Becomes true while inside the Cashier trigger.")]
    public bool canPayHere = false;

    public int Total {
        get { int t = 0; for (int i=0;i<items.Count;i++) t += items[i].price; return t; }
    }

    public void Add(string name, int price)
    {
        items.Add(new Item(name, price));
        onChanged?.Invoke();
    }

    public void Clear()
    {
        items.Clear();
        onChanged?.Invoke();
    }

    public bool TryPayUsingMenuMoney()
    {
        if (!canPayHere) return false;

        var menu = PlayerMenuController.Instance; // your money owner
        if (menu == null) { Debug.LogWarning("Basket: PlayerMenuController.Instance is null."); return false; }

        int total = Total;
        if (menu.Money < total) return false;

        // Deduct via your API (supports negatives & clamps to >= 0)
        menu.AddMoney(-total);  // :contentReference[oaicite:0]{index=0}

        Clear();
        onPaid?.Invoke();
        return true;
    }
}