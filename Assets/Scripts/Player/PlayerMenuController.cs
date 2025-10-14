using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// menu.CloseMenu();
// menu.OpenMenu();
// menu.ToggleMenu();
// menu.AddMoney(100);
// menu.AddGems(2);
// menu.AddKillScore();
// menu.AddStar();

public class PlayerMenuController : MonoBehaviour
{
    public static PlayerMenuController Instance { get; private set; }
    
    [SerializeField] private CanvasGroup menuCanvasGroup;   // optional
    [SerializeField] private bool lockAlphaWhileOpen = true;
    private List<CanvasGroup> _allGroups;
    
    [Header("Hide these while the menu is open")]
    [SerializeField] private GameObject[] hideWhileMenuOpen; // e.g., CrosshairRoot, TestosteroneBarRoot
    
    [Header("Menu Panel + Input")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private bool startClosed = true;
    [SerializeField] private bool pauseOnOpen = false;

    [Header("Counters")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text killsText;

    [Header("Stars (prefabs)")]
    [SerializeField] private Transform starsRoot;        // Parent container in your UI
    [SerializeField] private GameObject emptyStarPrefab; // grayscale
    [SerializeField] private GameObject fullStarPrefab;  // colored
    [SerializeField] private int totalStars = 5;
    [SerializeField] private int startingLevel = 1;      // start with first star colored
    [SerializeField, Range(0.1f, 3f)] private float starScale = 0.8f;


    [Header("Optional: auto-bind TestosteroneSystem to stars")]
    [SerializeField] private bool autoBindTestosterone = false;

    [Header("Cursor & behavior")]
    [SerializeField] private bool manageCursor = true;          // show/unlock cursor while menu open
    [SerializeField] private bool disablePlayerWhileOpen = true;
    [SerializeField] private MonoBehaviour[] toDisableOnOpen;   // e.g., your PlayerController, MouseLook, etc.
    [SerializeField] private GameObject firstSelected;          // optional: auto-select a button on open
    [SerializeField] private bool normalizeScaleOnOpen = true;
    [SerializeField] private bool disableAnimators = false;     // default OFF

    private Vector3 _originalScale;
    private CursorLockMode _prevLock;
    private bool _prevVisible;

    // runtime state
    [SerializeField, Min(0)] private int money = 0;   // editable in Inspector at runtime
    public int Money => money;                         // read-only accessor
    public int Gems  { get; private set; }
    public int Kills { get; private set; }
    public int StarLevel { get; private set; } // 0..totalStars

    private TestosteroneSystem _testosteroneSystem;

    // each slot holds both variants; we toggle active
    private struct StarSlot { public GameObject empty; public GameObject full; }
    private readonly List<StarSlot> _starSlots = new();

    void Awake()
    {
        Instance = this;
        
        money = Mathf.Max(0, money); // just ensures clamped from Inspector
        
        if (!menuPanel) { Debug.LogWarning("PlayerMenuController: menuPanel not assigned."); return; }

        _originalScale = menuPanel.transform.localScale; // capture before any toggles
        RefreshCounters();

        BuildStarSlots();
        SetStarLevel(Mathf.Clamp(startingLevel, 1, totalStars)); // level 1 = only first star filled

        if (autoBindTestosterone)
        {
            _testosteroneSystem = FindObjectOfType<TestosteroneSystem>();
            if (_testosteroneSystem != null)
            {
                SyncStarsFromNormalized(_testosteroneSystem.Normalized);
                _testosteroneSystem.OnValueChanged.AddListener(SyncStarsFromNormalized);
            }
        }

        CacheCanvasGroups();
        SetMenuVisible(!startClosed, force: true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        
        if (_testosteroneSystem != null)
            _testosteroneSystem.OnValueChanged.RemoveListener(SyncStarsFromNormalized);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleMenu();
    }

    void LateUpdate()
    {
        // If something else keeps fading it, clamp back to 1 while open
        if (lockAlphaWhileOpen && menuPanel && menuPanel.activeSelf && _allGroups != null)
        {
            for (int i = 0; i < _allGroups.Count; i++)
            {
                var cg = _allGroups[i];
                if (!cg) continue;
                if (cg.alpha != 1f) cg.alpha = 1f;
                if (!cg.interactable) cg.interactable = true;
                if (!cg.blocksRaycasts) cg.blocksRaycasts = true;
            }
        }
    }

    // -------- Public API --------
    public void OpenMenu()  => SetMenuVisible(true,  force: true);
    public void CloseMenu() => SetMenuVisible(false, force: true);
    public void ToggleMenu()
    {
        if (!menuPanel) return;
        SetMenuVisible(!menuPanel.activeSelf, force: true);
    }

    public void AddMoney(int amount)
    {
        money = Mathf.Max(0, money + amount);
        RefreshCounters();
    }
    public void AddGems(int amount)  { Gems  = Mathf.Max(0, Gems  + amount); RefreshCounters(); }
    public void AddKillScore(int amount = 1) { Kills = Mathf.Max(0, Kills + amount); RefreshCounters(); }

    // Light the next star (colored). Does nothing if already at max.
    public void AddStar() => SetStarLevel(Mathf.Min(StarLevel + 1, totalStars));

    // If you want to drive stars manually (not from TestosteroneSystem):
    public void SetStarLevel(int level)
    {
        StarLevel = Mathf.Clamp(level, 0, totalStars);
        for (int i = 0; i < _starSlots.Count; i++)
        {
            bool filled = i < StarLevel;
            if (_starSlots[i].empty) _starSlots[i].empty.SetActive(!filled);
            if (_starSlots[i].full)  _starSlots[i].full.SetActive(filled);
        }
    }
    // ----------------------------

    private void RefreshCounters()
    {
        if (moneyText) moneyText.text = Money.ToString();
        if (gemsText)  gemsText.text  = Gems.ToString();
        if (killsText) killsText.text = Kills.ToString();
    }

    private void CacheCanvasGroups()
    {
        if (!menuPanel) return;
        if (!menuCanvasGroup) menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
        _allGroups = new List<CanvasGroup>(menuPanel.GetComponentsInChildren<CanvasGroup>(true));
        if (menuCanvasGroup && !_allGroups.Contains(menuCanvasGroup)) _allGroups.Add(menuCanvasGroup);
    }

    private void SetMenuVisible(bool visible, bool force = false)
    {
        if (!menuPanel) return;

        if (force && disableAnimators)
        {
            foreach (var anim in menuPanel.GetComponentsInChildren<Animator>(true)) anim.enabled = false;
            foreach (var anim in menuPanel.GetComponentsInChildren<Animation>(true)) anim.enabled = false;
        }

        // Set active first so GetComponentsInChildren in LateUpdate include current UI
        menuPanel.SetActive(visible);

        // snap CanvasGroup state
        if (_allGroups != null)
        {
            float a = visible ? 1f : 0f;
            bool inter = visible, ray = visible;
            for (int i = 0; i < _allGroups.Count; i++)
            {
                var cg = _allGroups[i]; if (!cg) continue;
                cg.alpha = a; cg.interactable = inter; cg.blocksRaycasts = ray;
            }
        }

        // normalize scale if something tweened it
        if (normalizeScaleOnOpen && visible)
        {
            var rt = menuPanel.transform as RectTransform;
            if (rt) rt.localScale = Vector3.one; // or _originalScale if you prefer
        }

        // cursor handling
        if (manageCursor)
        {
            if (visible)
            {
                _prevLock = Cursor.lockState;
                _prevVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (firstSelected != null)
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es != null) es.SetSelectedGameObject(firstSelected);
                }
            }
            else
            {
                Cursor.lockState = _prevLock;
                Cursor.visible = _prevVisible;
            }
        }

        // disable gameplay scripts while menu is open
        if (disablePlayerWhileOpen && toDisableOnOpen != null)
        {
            foreach (var mb in toDisableOnOpen)
                if (mb) mb.enabled = !visible;
        }
        
        // Hide in-game UI while menu is open
        if (hideWhileMenuOpen != null)
        {
            bool showInGameUI = !visible;
            for (int i = 0; i < hideWhileMenuOpen.Length; i++)
                if (hideWhileMenuOpen[i]) hideWhileMenuOpen[i].SetActive(showInGameUI);
        }

        if (pauseOnOpen) Time.timeScale = visible ? 0f : 1f;
    }

    private void BuildStarSlots()
    {
        if (!starsRoot || !emptyStarPrefab || !fullStarPrefab) return;

        _starSlots.Clear();
        for (int i = starsRoot.childCount - 1; i >= 0; i--)
            Destroy(starsRoot.GetChild(i).gameObject);

        for (int i = 0; i < totalStars; i++)
        {
            var slotGO = new GameObject($"StarSlot_{i+1}", typeof(RectTransform));
            var slotRt = (RectTransform)slotGO.transform;
            slotRt.SetParent(starsRoot, false);

            // scale the slot (affects both empty/full children)
            slotRt.localScale = Vector3.one * starScale;

            slotRt.anchorMin = slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.anchoredPosition = Vector2.zero;

            var empty = Instantiate(emptyStarPrefab, slotRt);
            var full  = Instantiate(fullStarPrefab,  slotRt);

            foreach (var t in new[] { (RectTransform)empty.transform, (RectTransform)full.transform })
            {
                t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
                t.anchoredPosition = Vector2.zero;
                t.localScale = Vector3.one; // keep children at 1; slot handles overall scale
            }

            empty.SetActive(true);
            full.SetActive(false);
            _starSlots.Add(new StarSlot { empty = empty, full = full });
        }
    }

    private void SyncStarsFromNormalized(float normalized)
    {
        // Map normalized [0..1] to 1..totalStars (min 1 visible as requested)
        int level = Mathf.Max(1, Mathf.RoundToInt(normalized * totalStars));
        SetStarLevel(level);
    }
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // Clamp and refresh UI in edit & play mode when you tweak the Inspector value
        money = Mathf.Max(0, money);
        if (Application.isPlaying) RefreshCounters();
    }
#endif

}
