using UnityEngine;

[DisallowMultipleComponent]
public class ShopItem : MonoBehaviour
{
    [Header("Item")]
    public string itemName = "Rifle";
    public int price = 500;

    [Header("Highlight Mode")]
    public bool useOverlayMaterial = true;
    [Tooltip("Assign a bright unlit/additive material (e.g., URP Unlit Color)")]
    public Material overlayMaterial; // required if useOverlayMaterial = true

    [Header("Emission Fallback (if not using overlay)")]
    public Color emissionColor = new Color(1f, 0.9f, 0.2f);
    public float emissionIntensity = 1.8f;

    [Tooltip("Leave empty to auto-grab all child Renderers.")]
    public Renderer[] renderersToHighlight;

    // cache
    bool _isHighlighted;
    static readonly int _EmissionColor = Shader.PropertyToID("_EmissionColor");

    // Emission path state
    Material[][] _originalInstancedMats;

    void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<Renderer>(true);

        if (!useOverlayMaterial)
        {
            // Instance materials so we can enable keywords safely per object
            _originalInstancedMats = new Material[renderersToHighlight.Length][];
            for (int i = 0; i < renderersToHighlight.Length; i++)
            {
                var r = renderersToHighlight[i];
                var instanced = r.materials; // creates per-renderer instances
                _originalInstancedMats[i] = instanced;
            }
        }
    }

    public void SetHighlighted(bool on)
    {
        if (_isHighlighted == on) return;
        _isHighlighted = on;

        if (useOverlayMaterial)
        {
            ApplyOverlay(on);
        }
        else
        {
            ApplyEmission(on);
        }
    }

    void ApplyOverlay(bool on)
    {
        if (overlayMaterial == null)
        {
            Debug.LogWarning($"[ShopItem] {name} missing overlayMaterial. Falling back to emission.");
            useOverlayMaterial = false;
            ApplyEmission(on);
            return;
        }

        foreach (var r in renderersToHighlight)
        {
            if (!r) continue;
            var mats = r.sharedMaterials; // ok to use shared here; we’re only adding/removing an extra slot
            if (on)
            {
                // add overlay if not already present
                bool has = System.Array.Exists(mats, m => m == overlayMaterial);
                if (!has)
                {
                    var newMats = new Material[mats.Length + 1];
                    for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                    newMats[newMats.Length - 1] = overlayMaterial;
                    r.sharedMaterials = newMats;
                }
            }
            else
            {
                // remove overlay if present
                int idx = System.Array.FindIndex(mats, m => m == overlayMaterial);
                if (idx >= 0)
                {
                    var newMats = new System.Collections.Generic.List<Material>(mats);
                    newMats.RemoveAt(idx);
                    r.sharedMaterials = newMats.ToArray();
                }
            }
        }
    }

    void ApplyEmission(bool on)
    {
        Color c = emissionColor * emissionIntensity;

        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            var r = renderersToHighlight[i];
            if (!r) continue;

            var mats = r.materials; // per-renderer instances (we cached on Awake)
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (!mat) continue;

                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor(_EmissionColor, c);
                }
                else
                {
                    // turn off emission
                    mat.SetColor(_EmissionColor, Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
