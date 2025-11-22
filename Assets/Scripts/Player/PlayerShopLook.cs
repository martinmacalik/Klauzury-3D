using UnityEngine;
using System.Linq;

public class PlayerShopLook : MonoBehaviour
{
    [Header("Raycasting")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public LayerMask shopMask = ~0;

    [Header("UI")]
    public ShopTooltipUI tooltip;

    [Header("Input")]
    public KeyCode addToBasketKey = KeyCode.E;   // E to add
    public KeyCode payKey = KeyCode.E;           // E to pay when NOT aiming an item

    [Header("Stability")]
    public float reticleSphereRadius = 0.02f; // small spherecast helps with tiny colliders

    ShopItem _currentAim;
    Basket _basket;
    
    static int _lastAddFrame = -1;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!tooltip) Debug.LogWarning("[PlayerShopLook] Tooltip reference is missing.");

        // Be more flexible: look on this object first, then parents/children.
        _basket = GetComponent<Basket>();
        if (!_basket) _basket = GetComponentInParent<Basket>();
        if (!_basket) _basket = GetComponentInChildren<Basket>();
        if (!_basket) Debug.LogWarning("[PlayerShopLook] No Basket found. Add Basket to the player object.");
    }

    void Update()
    {
        UpdateAim();
        UpdateUI();
        HandleInput();
    }

    void UpdateAim()
    {
        ShopItem hitItem = null;

        if (cam)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            // Spherecast first (friendlier for small colliders), then fallback to raycast
            bool hit = Physics.SphereCast(ray, reticleSphereRadius, out RaycastHit info, interactDistance, shopMask, QueryTriggerInteraction.Collide);
            if (!hit) hit = Physics.Raycast(ray, out info, interactDistance, shopMask, QueryTriggerInteraction.Collide);

            if (hit)
            {
                hitItem = info.collider.GetComponentInParent<ShopItem>();
            }
        }

        if (_currentAim == hitItem) return;

        if (_currentAim) _currentAim.SetHighlighted(false);
        _currentAim = hitItem;
        if (_currentAim) _currentAim.SetHighlighted(true);
    }

    void UpdateUI()
    {
        if (!tooltip) return;

        if (_currentAim)
        {
            // Check if it's a free item (price = 0)
            if (_currentAim.price == 0)
            {
                // Free item → show pickup prompt without price
                string hint = $"Press {addToBasketKey} to pick up";
                tooltip.Show(_currentAim.itemName, 0, hint);
            }
            else
            {
                // Paid item → show basket prompt with price
                string hint = $"Press {addToBasketKey} to put in basket";
                tooltip.Show(_currentAim.itemName, _currentAim.price, hint);
            }
        }
        else
        {
            // Not aiming at an item → if we can pay, show a pay hint
            if (_basket != null && _basket.items != null && _basket.items.Count > 0 && _basket.canPayHere)
            {
                tooltip.Show("Checkout", _basket.Total, $"Press {payKey} to pay");
            }
            else
            {
                tooltip.Hide();
            }
        }
    }

    void HandleInput()
    {
        if (_basket == null) return;

        // Check if ShopInteractController already handled the interaction
        if (ShopInteractController.ConsumedInteractThisFrame) return;

        // 1) Add to basket when aiming at an item (OR pickup directly if free)
        if (_currentAim && Input.GetKeyDown(addToBasketKey))
        {
            // Prevent duplicate adds within same frame
            if (Time.frameCount == _lastAddFrame) return;
            _lastAddFrame = Time.frameCount;

            // FREE ITEM BYPASS: if price is 0, add directly to inventory and destroy
            if (_currentAim.price == 0)
            {
                PickupFreeItem(_currentAim);
            }
            else
            {
                // Normal flow: add to basket
                _basket.Add(_currentAim.itemName, _currentAim.price);
            }
        }

        // 2) Pay when NOT aiming at an item and pressing payKey
        if (!_currentAim && Input.GetKeyDown(payKey))
        {
            var inv = InventoryManager.Instance ?? FindFirstObjectByType<InventoryManager>();
            if (inv) inv.PurchaseBasket(_basket);
        }
    }

    void PickupFreeItem(ShopItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[PlayerShopLook] PickupFreeItem: item is null!");
            return;
        }

        Debug.Log($"[PlayerShopLook] Picking up free item: '{item.itemName}'");

        // Try to use InventoryManager first
        var invManager = InventoryManager.Instance;
        if (invManager == null)
        {
            invManager = FindFirstObjectByType<InventoryManager>();
        }
        if (invManager == null)
        {
            // Try to find even if inactive
            invManager = UnityEngine.Resources.FindObjectsOfTypeAll<InventoryManager>()
                .FirstOrDefault(m => m.gameObject.scene.name != null);
        }

        if (invManager != null)
        {
            invManager.TryAddByName(item.itemName);
            Debug.Log($"[PlayerShopLook] Added '{item.itemName}' via InventoryManager");
        }
        else
        {
            // Fallback: Add directly to backpack inventories
            Debug.LogWarning("[PlayerShopLook] InventoryManager not found - adding directly to backpack");

            // Try to determine category from database
            var db = FindFirstObjectByType<InventoryDatabase>();
            if (db == null)
            {
                // Try to find it as an asset
                db = UnityEngine.Resources.FindObjectsOfTypeAll<InventoryDatabase>().FirstOrDefault();
            }

            if (db != null && db.TryGet(item.itemName, out var entry))
            {
                if (entry.category == InventoryDatabase.ItemCategory.Gun)
                {
                    var wInv = WeaponInventory.Instance ?? FindFirstObjectByType<WeaponInventory>();
                    if (wInv != null)
                    {
                        wInv.AddWeapon(item.itemName);
                        Debug.Log($"[PlayerShopLook] Added '{item.itemName}' to WeaponInventory");
                    }
                }
                else // Generic, Keycard, or any other category goes to MiscInventory
                {
                    var mInv = MiscInventory.Instance ?? FindFirstObjectByType<MiscInventory>();
                    if (mInv != null)
                    {
                        mInv.AddItem(item.itemName, 1);
                        Debug.Log($"[PlayerShopLook] Added '{item.itemName}' to MiscInventory");
                    }
                }
            }
            else
            {
                Debug.LogError($"[PlayerShopLook] Could not determine category for '{item.itemName}' - database not found or item not in database");
            }
        }

        // Destroy the shop item object
        if (_currentAim == item)
        {
            _currentAim.SetHighlighted(false);
            _currentAim = null;
        }
        
        Destroy(item.gameObject);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Camera c = cam ? cam : Camera.main;
        if (!c) return;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(c.transform.position + c.transform.forward * Mathf.Min(interactDistance, 0.5f), reticleSphereRadius);
        Gizmos.DrawLine(c.transform.position, c.transform.position + c.transform.forward * interactDistance);
    }
#endif
}
