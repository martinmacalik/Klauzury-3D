using UnityEngine;
public class PlayerHackingLook : MonoBehaviour
{
    [Header("Raycasting")]
    public Camera cam;
    public float interactDistance = 3.0f;
    public LayerMask interactableMask = ~0;
    [Header("UI")]
    public ShopTooltipUI tooltip; // Reuse your existing tooltip
    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;
    [Header("Stability")]
    public float reticleSphereRadius = 0.02f;
    private HackingBox _currentAim;
    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!tooltip) tooltip = FindFirstObjectByType<ShopTooltipUI>();
    }
    void Update()
    {
        UpdateAim();
        UpdateUI();
        HandleInput();
    }
    void UpdateAim()
    {
        HackingBox hitBox = null;
        if (cam)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            // Spherecast first (friendlier for small colliders), then fallback to raycast
            bool hit = Physics.SphereCast(ray, reticleSphereRadius, out RaycastHit info, interactDistance, interactableMask, QueryTriggerInteraction.Collide);
            if (!hit) hit = Physics.Raycast(ray, out info, interactDistance, interactableMask, QueryTriggerInteraction.Collide);
            if (hit)
            {
                hitBox = info.collider.GetComponentInParent<HackingBox>();
            }
        }
        if (_currentAim == hitBox) return;
        // Unhighlight previous
        if (_currentAim) _currentAim.SetHighlighted(false);
        // Highlight new
        _currentAim = hitBox;
        if (_currentAim) _currentAim.SetHighlighted(true);
    }
    void UpdateUI()
    {
        if (!tooltip) return;
        if (_currentAim)
        {
            string prompt = _currentAim.GetPromptText();
            bool canInteract = _currentAim.CanInteract();
            // Show tooltip with colored text based on availability
            tooltip.Show("Hacking Terminal", 0, prompt);
        }
        else
        {
            tooltip.Hide();
        }
    }
    void HandleInput()
    {
        if (_currentAim && Input.GetKeyDown(interactKey))
        {
            _currentAim.Interact();
        }
    }
}
