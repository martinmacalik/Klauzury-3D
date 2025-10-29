using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryPageSwitcher : MonoBehaviour
{
    [Header("Toggles (UGUI)")]
    public Toggle homeToggle;        // Canvas-based Toggle
    public Toggle backpackToggle;    // Canvas-based Toggle

    [Header("Pages (roots)")]
    public GameObject homePageRoot;      // parent of your home layout
    public GameObject backpackPageRoot;  // parent of your backpack layout

    [Header("Layout Rebuild")]
    [Tooltip("Root RectTransform that contains your pages (Grid/Vertical/Horizontal Layout, etc).")]
    public RectTransform layoutRoot;
    [Tooltip("Force rebuild when switching pages. Fixes most ‘layout not updating’ issues.")]
    public bool forceRebuild = true;

    [Header("Optional: also drive CanvasGroups for proper raycasts")]
    public bool useCanvasGroups = true;

    void Awake()
    {
        if (homeToggle)     homeToggle.onValueChanged.AddListener(OnHomeToggled);
        if (backpackToggle) backpackToggle.onValueChanged.AddListener(OnBackpackToggled);

        // Initialize based on whichever toggle starts ON.
        ApplyState();
    }

    void OnDestroy()
    {
        if (homeToggle)     homeToggle.onValueChanged.RemoveListener(OnHomeToggled);
        if (backpackToggle) backpackToggle.onValueChanged.RemoveListener(OnBackpackToggled);
    }

    void OnHomeToggled(bool on)
    {
        if (on) ShowPage(home: true);
    }

    void OnBackpackToggled(bool on)
    {
        if (on) ShowPage(home: false);
    }

    void ApplyState()
    {
        bool homeOn = homeToggle && homeToggle.isOn;
        bool backOn = backpackToggle && backpackToggle.isOn;

        // If both off (ToggleGroup allowSwitchOff?), default to Home
        if (!homeOn && !backOn)
        {
            homeOn = true;
            if (homeToggle) homeToggle.isOn = true;
        }

        ShowPage(home: homeOn);
    }

    void ShowPage(bool home)
    {
        SetVisible(homePageRoot,    home);
        SetVisible(backpackPageRoot,!home);

        if (forceRebuild) StartCoroutine(CoForceRebuild());
    }

    void SetVisible(GameObject root, bool visible)
    {
        if (!root) return;

        // 1) Toggle active
        root.SetActive(visible);

        // 2) (Optional) CanvasGroup for alpha & raycasts
        if (useCanvasGroups)
        {
            var cg = root.GetComponent<CanvasGroup>();
            if (!cg) cg = root.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }

    IEnumerator CoForceRebuild()
    {
        // Let SetActive settle
        yield return null;
        if (layoutRoot)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }

        // One more pass for nested layouts/fitters
        yield return null;
        if (layoutRoot)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }
}
