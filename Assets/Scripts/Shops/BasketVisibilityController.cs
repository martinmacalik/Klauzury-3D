using UnityEngine;

public class BasketVisibilityController : MonoBehaviour
{
    public Basket basket;
    public GameObject panelRoot; // the whole basket panel (usually this same GO)

    [Header("Input")]
    public KeyCode clearKey = KeyCode.R;

    void OnEnable()
    {
        if (!panelRoot) panelRoot = gameObject;
        if (basket) basket.onChanged.AddListener(Refresh);
        Refresh();
    }

    void OnDisable()
    {
        if (basket) basket.onChanged.RemoveListener(Refresh);
    }

    void Update()
    {
        if (basket != null && Input.GetKeyDown(clearKey))
            basket.Clear(); // also fires onChanged
    }

    void Refresh()
    {
        if (!basket || !panelRoot) return;
        bool show = basket.items.Count > 0;
        panelRoot.SetActive(show);
    }
}