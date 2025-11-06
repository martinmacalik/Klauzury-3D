using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryPageSwitcher : MonoBehaviour
{
    [Header("Toggles (UGUI)")]
    public Toggle homeToggle;        // Canvas-based Toggle
    public Toggle backpackToggle;    // Canvas-based Toggle
    public Toggle miscelaniousBackpackToggle; // New
    public Toggle infoToggle;                   // New

    [Header("Pages (roots)")]
    public GameObject homePageRoot;      // parent of your home layout
    public GameObject backpackPageRoot;  // parent of your backpack layout
    public GameObject miscelaniousBackpackPageRoot; // New
    public GameObject infoPageRoot;                 // New

    [Header("Layout Rebuild")]
    [Tooltip("Root RectTransform that contains your pages (Grid/Vertical/Horizontal Layout, etc).")]
    public RectTransform layoutRoot;
    [Tooltip("Force rebuild when switching pages. Fixes most 'layout not updating' issues.")]
    public bool forceRebuild = true;

    [Header("Optional: also drive CanvasGroups for proper raycasts")]
    public bool useCanvasGroups = true;

    [Header("Auto-Reset to Home")]
    [Tooltip("Always reset to Home page when the inventory/menu opens.")]
    public bool resetToHomeOnOpen = true;

    enum Page { Home, Backpack, MiscelaniousBackpack, Info }

    void Awake()
    {
        if (homeToggle)                  homeToggle.onValueChanged.AddListener(OnHomeToggled);
        if (backpackToggle)              backpackToggle.onValueChanged.AddListener(OnBackpackToggled);
        if (miscelaniousBackpackToggle)  miscelaniousBackpackToggle.onValueChanged.AddListener(OnMiscToggled);
        if (infoToggle)                  infoToggle.onValueChanged.AddListener(OnInfoToggled);

        // Initialize based on whichever toggle starts ON.
        ApplyState();
    }

    void OnEnable()
    {
        // Reset to Home page every time the menu/inventory becomes active
        if (resetToHomeOnOpen)
        {
            ResetToHome();
        }
    }

    void OnDestroy()
    {
        if (homeToggle)                  homeToggle.onValueChanged.RemoveListener(OnHomeToggled);
        if (backpackToggle)              backpackToggle.onValueChanged.RemoveListener(OnBackpackToggled);
        if (miscelaniousBackpackToggle)  miscelaniousBackpackToggle.onValueChanged.RemoveListener(OnMiscToggled);
        if (infoToggle)                  infoToggle.onValueChanged.RemoveListener(OnInfoToggled);
    }

    void OnHomeToggled(bool on)
    {
        if (on) ShowPage(Page.Home);
    }

    void OnBackpackToggled(bool on)
    {
        if (on) ShowPage(Page.Backpack);
    }

    void OnMiscToggled(bool on)
    {
        if (on) ShowPage(Page.MiscelaniousBackpack);
    }

    void OnInfoToggled(bool on)
    {
        if (on) ShowPage(Page.Info);
    }

    void ApplyState()
    {
        bool homeOn  = homeToggle && homeToggle.isOn;
        bool backOn  = backpackToggle && backpackToggle.isOn;
        bool miscOn  = miscelaniousBackpackToggle && miscelaniousBackpackToggle.isOn;
        bool infoOn  = infoToggle && infoToggle.isOn;

        // If all off (ToggleGroup allowSwitchOff?), default to Home
        if (!homeOn && !backOn && !miscOn && !infoOn)
        {
            ResetToHome();
            return;
        }

        // Pick whichever is currently ON (priority order: Home, Backpack, Misc, Info)
        if (homeOn) { ShowPage(Page.Home); return; }
        if (backOn) { ShowPage(Page.Backpack); return; }
        if (miscOn) { ShowPage(Page.MiscelaniousBackpack); return; }
        if (infoOn) { ShowPage(Page.Info); return; }
    }

    /// <summary>
    /// Force the Home toggle on and show Home page
    /// </summary>
    public void ResetToHome()
    {
        if (homeToggle && !homeToggle.isOn)
        {
            homeToggle.isOn = true; // This will trigger OnHomeToggled
        }
        else
        {
            // If toggle is already on, just show the page directly
            ShowPage(Page.Home);
        }
    }

    // Back-compat wrapper used by the original two toggles
    void ShowPage(bool home)
    {
        ShowPage(home ? Page.Home : Page.Backpack);
    }

    void ShowPage(Page page)
    {
        SetVisible(homePageRoot,                 page == Page.Home);
        SetVisible(backpackPageRoot,             page == Page.Backpack);
        SetVisible(miscelaniousBackpackPageRoot, page == Page.MiscelaniousBackpack);
        SetVisible(infoPageRoot,                 page == Page.Info);

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
