using UnityEngine;

public class PlayerShopLook : MonoBehaviour
{
    [Header("Raycasting")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public LayerMask shopMask = ~0;

    [Header("UI")]
    public ShopTooltipUI tooltip;

    [Header("Input")]
    public KeyCode addToBasketKey = KeyCode.E;
    public KeyCode payKey = KeyCode.F;

    [Header("Stability")]
    public float reticleSphereRadius = 0.02f; // small spherecast helps with tiny colliders

    ShopItem _currentAim;
    Basket _basket;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!tooltip) Debug.LogWarning("[PlayerShopLook] Tooltip reference is missing.");
        _basket = GetComponent<Basket>(); // must be on the player
        if (!_basket) Debug.LogWarning("[PlayerShopLook] No Basket found on player.");
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

            bool hit = Physics.SphereCast(ray, reticleSphereRadius, out RaycastHit info, interactDistance, shopMask, QueryTriggerInteraction.Collide);
            if (!hit) hit = Physics.Raycast(ray, out info, interactDistance, shopMask, QueryTriggerInteraction.Collide);

            if (hit) hitItem = info.collider.GetComponentInParent<ShopItem>();
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
            string hint = $"Press {addToBasketKey} to put in basket";
            tooltip.Show(_currentAim.itemName, _currentAim.price, hint);
        }
        else
        {
            tooltip.Hide();
        }
    }

    void HandleInput()
    {
        if (_basket == null) return;

        if (Input.GetKeyDown(addToBasketKey) && _currentAim)
        {
            _basket.Add(_currentAim.itemName, _currentAim.price);
            // Optional: sound or tiny flash on the item
        }

        if (Input.GetKeyDown(payKey))
        {
            bool ok = _basket.TryPayUsingMenuMoney(); // uses PlayerMenuController.Instance
            if (!ok)
            {
                // Optional feedback: not in cashier zone OR not enough money.
                // You could blink Basket UI or play a deny sound here.
            }
        }
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
