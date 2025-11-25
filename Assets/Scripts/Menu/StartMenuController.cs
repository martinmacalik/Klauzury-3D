using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StartMenuController : MonoBehaviour
{
    public static StartMenuController Instance { get; private set; }
    public bool HasStarted => _started;

    [Header("Menu")]
    public GameObject menuRig;          // root of the flying menu
    public CanvasGroup menuCanvasGroup; // for fade
    public KeyCode debugStartKey = KeyCode.Return; // optional: press Enter to start

    [Header("Cameras (Non-Cinemachine)")]
    public Camera menuCamera;           // active at boot
    public Camera playerCamera;         // becomes active on Start

    [Header("Player control to disable until Start")]
    public List<Behaviour> disableUntilStart = new List<Behaviour>(); // movement, look, WeaponHotkeys, SimpleGun, etc.
    public List<GameObject> objectsUntilStart = new List<GameObject>(); // entire objects to disable if needed

    [Header("Cursor & Time")]
    public bool unlockCursorInMenu = true;   // cursor visible for clicking Start
    public bool keepTimeRunning = true;      // leave 1.0 so city sim runs under menu

    bool _started;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        // Cameras
        if (menuCamera) menuCamera.gameObject.SetActive(true);
        if (playerCamera) playerCamera.gameObject.SetActive(false);

        // Disable player control
        foreach (var b in disableUntilStart) if (b) b.enabled = false;
        foreach (var go in objectsUntilStart) if (go) go.SetActive(false);

        // UI
        if (menuRig) menuRig.SetActive(true);
        if (menuCanvasGroup) menuCanvasGroup.alpha = 1f;

        // Cursor
        if (unlockCursorInMenu) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }

        // Time
        if (!keepTimeRunning) Time.timeScale = 0f;

        // Guns safe
        WeaponHotkeys.GunIsReady = false;

        // Ensure only one AudioListener (disable on inactive cam)
        var menuAL   = menuCamera   ? menuCamera.GetComponent<AudioListener>()   : null;
        var playerAL = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;
        if (menuAL && playerAL) playerAL.enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Keep cursor visible and unlocked while in menu
        if (!_started && unlockCursorInMenu)
        {
            if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        if (!_started && Input.GetKeyDown(debugStartKey))
            StartGame();
    }

    // Hook this to the Start button OnClick
    public void StartGame()
    {
        if (_started) return;
        _started = true;
        StartCoroutine(DoStartSequence());
    }

    // Hook this to the Quit button OnClick
    public void QuitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    IEnumerator DoStartSequence()
    {
        // Reset testosterone at the start of the game
        if (TestosteroneSystem.Instance != null)
            TestosteroneSystem.Instance.ResetToStart();

        // Fade menu out quickly
        yield return StartCoroutine(Fade(menuCanvasGroup, 0f, 0.25f));

        // Switch cameras
        if (menuCamera) menuCamera.gameObject.SetActive(false);
        if (playerCamera) playerCamera.gameObject.SetActive(true);

        // AudioListener sanity
        var menuAL   = menuCamera   ? menuCamera.GetComponent<AudioListener>()   : null;
        var playerAL = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;
        if (menuAL) menuAL.enabled = false;
        if (playerAL) playerAL.enabled = true;

        // Hide menu rig
        if (menuRig) menuRig.SetActive(false);

        // Re-enable player control
        foreach (var go in objectsUntilStart) if (go) go.SetActive(true);
        foreach (var b in disableUntilStart) if (b) b.enabled = true;

        // Cursor + time
        Cursor.visible = false; 
        Cursor.lockState = CursorLockMode.Locked;
        if (!keepTimeRunning) Time.timeScale = 1f;

        // Guns ok now
        WeaponHotkeys.GunIsReady = true;
    }

    // Call this to reset the game back to the menu
    public void ResetToMenu()
    {
        if (!_started) return; // Already in menu
        _started = false;

        // Do immediate setup first (without coroutine)
        DoImmediateReset();

        // Now start the fade coroutine (GameObject should be active now)
        StartCoroutine(DoResetFade());
    }

    void DoImmediateReset()
    {
        // Disable player control immediately
        foreach (var b in disableUntilStart) if (b) b.enabled = false;
        foreach (var go in objectsUntilStart) if (go) go.SetActive(false);

        // Guns safe
        WeaponHotkeys.GunIsReady = false;

        // Switch cameras
        if (playerCamera) playerCamera.gameObject.SetActive(false);
        if (menuCamera) menuCamera.gameObject.SetActive(true);

        // AudioListener sanity
        var menuAL   = menuCamera   ? menuCamera.GetComponent<AudioListener>()   : null;
        var playerAL = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;
        if (menuAL) menuAL.enabled = true;
        if (playerAL) playerAL.enabled = false;

        // Show menu rig - CRITICAL: do this before trying to start coroutine
        if (menuRig) menuRig.SetActive(true);

        // Ensure this GameObject is also active
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        // Cursor
        if (unlockCursorInMenu) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }

        // Reset testosterone to start value
        if (TestosteroneSystem.Instance != null)
            TestosteroneSystem.Instance.ResetToStart();
    }

    IEnumerator DoResetFade()
    {
        // Fade menu in
        if (menuCanvasGroup) menuCanvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(menuCanvasGroup, 1f, 0.5f));
    }

    IEnumerator Fade(CanvasGroup cg, float to, float seconds)
    {
        if (!cg) yield break;
        float from = cg.alpha, t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        cg.alpha = to;
    }
}
