using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class ShopTooltipUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup group;      // your existing one
    public TMP_Text line1;         // item name
    public TMP_Text line2;         // $price
    public TMP_Text line3;         // "Press E to put in basket"

    [Header("Behavior")]
    public float fadeSpeed = 10f;
    public bool scalePunch = true;

    float _targetAlpha = 0f;
    Vector3 _baseScale;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (!group) group = gameObject.AddComponent<CanvasGroup>();
        _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
    }

    void Update()
    {
        if (!Mathf.Approximately(group.alpha, _targetAlpha))
        {
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, _targetAlpha, fadeSpeed * dt);

            if (scalePunch && _targetAlpha > 0.5f)
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 12f * dt);

            if (Mathf.Approximately(group.alpha, _targetAlpha))
            {
                bool on = _targetAlpha >= 0.99f;
                group.interactable = on;
                group.blocksRaycasts = on;
                if (!on) transform.localScale = _baseScale;
            }
        }
    }

    public void Show(string name, int price, string hint = "Press E to put in basket")
    {
        if (line1) line1.text = name ?? "";
        if (line2) line2.text = $"${price}";
        if (line3) line3.text = hint ?? "";
        if (scalePunch && group.alpha < 0.01f) transform.localScale = _baseScale * 1.02f;
        _targetAlpha = 1f;
    }

    public void Hide() => _targetAlpha = 0f;
}