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
            tooltip.Show(_current.itemName, _current.price, "Press E to put in basket");
        }
        else if (tooltip)
        {
            tooltip.Hide();
        }

        // 4) Add to basket on E
        if (_current && Input.GetKeyDown(addKey))
        {
            if (basket != null)
            {
                string name = _current.itemName;
                int price   = _current.price;

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
