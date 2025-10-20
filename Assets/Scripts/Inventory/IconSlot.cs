using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IconSlot : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;      // assign the Image on this slot
    public Sprite emptySprite;   // optional placeholder
    public Color emptyColor = new Color(1,1,1,0.25f);
    public Color filledColor = Color.white;

    [Header("Size (% of slot’s shortest side)")]
    [Range(0.1f, 2f)] public float emptyPercent  = 0.85f;
    [Range(0.1f, 2f)] public float filledPercent = 1.15f;
    public bool preserveAspect = true;

    bool _filled;
    RectTransform _iconRT;
    RectTransform _slotRT;
    LayoutElement _layoutElement; // optional, only used if a LayoutGroup controls children

    void Awake()
    {
        if (!iconImage) iconImage = GetComponentInChildren<Image>(true);
        _iconRT = iconImage ? iconImage.rectTransform : null;
        _slotRT = transform as RectTransform;
        _layoutElement = iconImage ? iconImage.GetComponent<LayoutElement>() : null;

        if (iconImage) iconImage.preserveAspect = preserveAspect;
        EnsureCenteredAnchors();
        SetEmptyVisual();
    }

    public bool IsEmpty => !_filled;

    public void Clear()
    {
        _filled = false;
        SetEmptyVisual();
    }

    public void SetIcon(Sprite sprite)
    {
        _filled = sprite != null;
        if (!iconImage) { Debug.LogError($"[IconSlot:{name}] iconImage is NOT assigned."); return; }

        iconImage.sprite  = _filled ? sprite : emptySprite;
        iconImage.color   = _filled ? filledColor : emptyColor;
        iconImage.enabled = iconImage.sprite != null;

        ApplySize(); // <- key change
        Debug.Log($"[IconSlot:{name}] SetIcon -> sprite='{(iconImage.sprite ? iconImage.sprite.name : "null")}', size={_iconRT.sizeDelta}");
    }

    void SetEmptyVisual()
    {
        if (!iconImage) return;
        iconImage.sprite  = emptySprite;
        iconImage.color   = emptyColor;
        iconImage.enabled = emptySprite != null;
        ApplySize(); // <- key change
    }

    void EnsureCenteredAnchors()
    {
        if (!_iconRT) return;
        // make sure the icon is NOT stretched by anchors
        _iconRT.anchorMin = _iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        _iconRT.pivot     = new Vector2(0.5f, 0.5f);
        _iconRT.anchoredPosition = Vector2.zero;
        // do not rely on localScale anymore
        _iconRT.localScale = Vector3.one;
    }

    void ApplySize()
    {
        if (_iconRT == null || _slotRT == null) return;

        // size based on the slot’s shortest side * percent
        float side  = Mathf.Min(_slotRT.rect.width, _slotRT.rect.height);
        float mult  = _filled ? filledPercent : emptyPercent;
        float size  = Mathf.Max(0f, side * mult);
        Vector2 v   = new Vector2(size, size);

        // if a LayoutGroup is controlling children, prefer LayoutElement
        if (_layoutElement != null)
        {
            _layoutElement.preferredWidth  = size;
            _layoutElement.preferredHeight = size;
        }

        _iconRT.sizeDelta = v; // explicit pixel size (anchors centered)
    }

    // keep size correct if parent/slot is resized at runtime
    void OnRectTransformDimensionsChange()
    {
        ApplySize();
    }
}
