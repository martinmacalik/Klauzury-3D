using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class ShopInteractController : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // your main camera
    public Basket basket;              // your existing Basket
    public ShopTooltipUI tooltip;      // the tooltip UI you uploaded
    public LayerMask hittableLayers = ~0;

    [Header("Settings")]
    public float maxDistance = 4.0f;   // how far you can aim at items
    public KeyCode addKey = KeyCode.E; // press E to add to basket

    // One-frame flag to tell other systems (like checkout) we've consumed E
    public static bool ConsumedInteractThisFrame { get; private set; }

    ShopItem _current;

    void Reset()
    {
        if (!cam) cam = Camera.main;
        if (!basket) basket = FindObjectOfType<Basket>();
        if (!tooltip) tooltip = FindObjectOfType<ShopTooltipUI>(true);
    }

    void Update()
    {
        ConsumedInteractThisFrame = false;

        // 1) Find what we're aiming at
        var hitItem = RaycastForItem();

        // 2) Handle highlight transitions
        if (_current != hitItem)
        {
            if (_current) _current.SetHighlighted(false);
            _current = hitItem;
            if (_current) _current.SetHighlighted(true);
        }

        // 3) Tooltip
        if (_current && tooltip)
        {
            // Show different prompt for free items
            if (_current.price == 0)
            {
                tooltip.Show(_current.itemName, 0, "Press E to pick up");
            }
            else
            {
                tooltip.Show(_current.itemName, _current.price, "Press E to put in basket");
            }
        }
        else if (tooltip)
        {
            tooltip.Hide();
        }

        // 4) Add to basket on E (or pickup directly if free)
        if (_current && Input.GetKeyDown(addKey))
        {
            string name = _current.itemName;
            int price = _current.price;

            // FREE ITEM BYPASS: if price is 0, add directly to inventory and destroy
            if (price == 0)
            {
                PickupFreeItem(_current);
                ConsumedInteractThisFrame = true;
                return;
            }

            // Normal paid items go into basket
            if (basket != null)
            {
                // NEW: disallow adding if already owned
                var wInv = WeaponInventory.Instance;
                if (wInv && wInv.CountOf(name) > 0)
                {
                    if (tooltip) tooltip.Show(name, price, "Already owned");
                    return;
                }

                // NEW: disallow duplicates in the basket
                bool alreadyInBasket = basket.items != null && basket.items.Any(it => it.name == name);
                if (alreadyInBasket)
                {
                    if (tooltip) tooltip.Show(name, price, "Already in basket");
                    return;
                }

                // OK to add
                basket.Add(name, price);
                ConsumedInteractThisFrame = true;
                if (tooltip) tooltip.Show(name, price, "Added to basket!");
            }
            else
            {
                Debug.LogWarning("No Basket reference on ShopInteractController.");
            }
        }
    }

    void PickupFreeItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ShopInteract] PickupFreeItem: item is null!");
            return;
        }

        Debug.Log($"[ShopInteract] Picking up free item: '{item.itemName}'");

        // Try to use InventoryManager first
        var invManager = InventoryManager.Instance;
        if (invManager == null)
        {
            invManager = FindFirstObjectByType<InventoryManager>();
        }
        if (invManager == null)
        {
            // Try to find even if inactive
            invManager = Resources.FindObjectsOfTypeAll<InventoryManager>()
                .FirstOrDefault(m => m.gameObject.scene.name != null);
        }
        
        if (invManager != null)
        {
            invManager.TryAddByName(item.itemName);
            Debug.Log($"[ShopInteract] Added '{item.itemName}' via InventoryManager");
        }
        else
        {
            // Fallback: Add directly to backpack inventories
            Debug.LogWarning("[ShopInteract] InventoryManager not found - adding directly to backpack");
            
            // Try to determine category from database
            var db = FindFirstObjectByType<InventoryDatabase>();
            if (db == null)
            {
                // Try to find it as an asset
                db = Resources.FindObjectsOfTypeAll<InventoryDatabase>().FirstOrDefault();
            }
            
            if (db != null && db.TryGet(item.itemName, out var entry))
            {
                if (entry.category == InventoryDatabase.ItemCategory.Gun)
                {
                    var wInv = WeaponInventory.Instance ?? FindFirstObjectByType<WeaponInventory>();
                    if (wInv != null)
                    {
                        wInv.AddWeapon(item.itemName);
                        Debug.Log($"[ShopInteract] Added '{item.itemName}' to WeaponInventory");
                    }
                }
                else // Generic, Keycard, or any other category goes to MiscInventory
                {
                    var mInv = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
                    if (mInv != null)
                    {
                        mInv.AddItem(item.itemName, 1);
                        Debug.Log($"[ShopInteract] Added '{item.itemName}' to MiscInventory");
                    }
                }
            }
            else
            {
                Debug.LogError($"[ShopInteract] Could not determine category for '{item.itemName}' - database not found or item not in database");
            }
        }

        // Destroy the shop item object
        if (_current == item)
        {
            _current.SetHighlighted(false);
            _current = null;
        }
        
        Destroy(item.gameObject);
    }

    ShopItem RaycastForItem()
    {
        if (!cam) return null;

        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, maxDistance, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<ShopItem>();
        }
        return null;
    }

    void OnDisable()
    {
        if (_current) { _current.SetHighlighted(false); _current = null; }
        if (tooltip) tooltip.Hide();
    }
}
