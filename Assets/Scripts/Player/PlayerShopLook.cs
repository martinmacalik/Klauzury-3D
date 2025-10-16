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
            // Aiming at an item → show “add” hint
            string hint = $"Press {addToBasketKey} to put in basket";
            tooltip.Show(_currentAim.itemName, _currentAim.price, hint);
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

        // 1) Add to basket when aiming at an item
        if (_currentAim && Input.GetKeyDown(addToBasketKey))
        {
            // Debounce: if some other script already added this frame, skip
            if (Time.frameCount == _lastAddFrame) return;

            _basket.Add(_currentAim.itemName, _currentAim.price); // updates UI & onChanged
            _lastAddFrame = Time.frameCount;                       // mark this frame as consumed
            return;
        }

        // 2) Pay when NOT aiming at an item (if you kept this behavior)
        if (!_currentAim && Input.GetKeyDown(payKey))
        {
            _basket.TryPayUsingMenuMoney();
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
