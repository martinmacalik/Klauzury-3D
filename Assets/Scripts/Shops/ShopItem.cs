using UnityEngine;

[DisallowMultipleComponent]
public class ShopItem : MonoBehaviour
{
    [Header("Item")]
    public string itemName = "Rifle";
    public int price = 500;
    
    [Header("UI Icon")]
    public Sprite iconSprite;

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

    // Store original materials for overlay mode
    Material[][] _originalMaterials;
    // Emission path state
    Material[][] _originalInstancedMats;

    void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
        {
            // Auto-grab only 3D renderers, exclude UI renderers
            var allRenderers = GetComponentsInChildren<Renderer>(true);
            var filtered = new System.Collections.Generic.List<Renderer>();
            
            foreach (var r in allRenderers)
            {
                // Only include MeshRenderer and SkinnedMeshRenderer (3D objects)
                // Exclude UI.Image, UI.RawImage, etc which use CanvasRenderer
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    filtered.Add(r);
                }
            }
            
            renderersToHighlight = filtered.ToArray();
            
            if (renderersToHighlight.Length == 0)
            {
                Debug.LogWarning($"[ShopItem] '{name}' has no 3D renderers to highlight. Assign manually or add MeshRenderer/SkinnedMeshRenderer.", this);
            }
        }

        // Store original materials for both modes
        _originalMaterials = new Material[renderersToHighlight.Length][];
        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            var r = renderersToHighlight[i];
            if (r) _originalMaterials[i] = r.sharedMaterials;
        }

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

        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            var r = renderersToHighlight[i];
            if (!r) continue;
            
            if (on)
            {
                // Replace ALL materials with the overlay material
                // This covers all 4 submeshes (metal, hologram, button, light)
                int materialCount = _originalMaterials[i].Length;
                var newMats = new Material[materialCount];
                for (int m = 0; m < materialCount; m++)
                {
                    newMats[m] = overlayMaterial;
                }
                r.sharedMaterials = newMats;
            }
            else
            {
                // Restore original materials
                if (_originalMaterials[i] != null)
                {
                    r.sharedMaterials = _originalMaterials[i];
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
