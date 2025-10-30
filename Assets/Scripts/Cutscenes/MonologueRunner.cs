using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// If you use the new Input System, uncomment the next line and add the package.
// using UnityEngine.InputSystem;

public class MonologueRunner : MonoBehaviour
{
    [TextArea(2, 6)]
    public List<string> lines;

    public Transform speakerHead;            // optional
    public SpeechBubble bubblePrefab;        // optional if you already have one in children
    public bool instantiatePrefab = false;

    [Header("Advance")]
    public bool advanceOnKey = true;
    public KeyCode advanceKey = KeyCode.Space;
    public bool autoAdvance = false;
    public float holdTimePerLine = 1.5f;

    [Header("Gameplay Blocking")]
    [Tooltip("Drag any movement/camera/inventory scripts here (Behaviours) to disable during monologue.")]
    public List<Behaviour> disableDuringMonologue = new List<Behaviour>();

    //[Tooltip("If assigned, we'll switch away from Gameplay map during monologue and restore after.")]
    public PlayerMovement playerMovement; // drag your Player object here
    bool prevCanMove, prevCanLook;
    
    [Tooltip("Action map used during gameplay (e.g., 'Gameplay').")]
    public string gameplayActionMap = "Gameplay";
    [Tooltip("Action map to use during monologue (e.g., 'UI'). Leave empty to just disable PlayerInput.")]
    public string monologueActionMap = "UI";
    public bool switchActionMaps = false;    // set true if using PlayerInput + action maps

    [Header("Cursor")]
    public bool showCursorDuringMonologue = true;

    SpeechBubble bubble;
    Coroutine runRoutine;

    // State storage so we can restore cleanly
    struct BehaviourState { public Behaviour comp; public bool wasEnabled; }
    List<BehaviourState> cachedStates = new List<BehaviourState>();
    // string prevActionMap; // for Input System
    bool prevCursorVisible;
    CursorLockMode prevCursorLock;

    void Start()
    {
        if (!instantiatePrefab && !bubble)
            bubble = GetComponentInChildren<SpeechBubble>(true);

        if (!speakerHead)
        {
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                var n = t.name.ToLower();
                if (n.Contains("head") || n.Contains("bubbleanchor")) { speakerHead = t; break; }
            }
        }

        PlayMonologue(); // auto-start
    }

    [ContextMenu("Play Monologue")]
    public void PlayMonologue()
    {
        if (runRoutine != null) StopCoroutine(runRoutine);
        runRoutine = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (lines == null || lines.Count == 0) yield break;

        if (!bubble)
        {
            if (instantiatePrefab && bubblePrefab)
                bubble = Instantiate(bubblePrefab, transform);
            else
            {
                Debug.LogWarning("MonologueRunner: No SpeechBubble assigned/child found.");
                yield break;
            }
        }

        bubble.followTarget = speakerHead;
        bubble.gameObject.SetActive(true);

        BeginBlockGameplay();

        int i = 0;

        // Start first line
        bubble.TypeOut(lines[i]);

        // helper local function
        IEnumerator WaitForKeyRelease(KeyCode key)
        {
            // ensure we don't consume multiple lines on one held press
            while (Input.GetKey(key)) yield return null;
        }

        while (true)
        {
            // If we’re auto-advancing (optional), you can keep your old hold timer here.
            // But per your request, we focus on key-based advance:
            if (advanceOnKey && Input.GetKeyDown(advanceKey))
            {
                if (bubble.IsTyping)
                {
                    // 1) Reveal immediately
                    bubble.RevealAll();
                    // 2) Debounce this press so the same key-down doesn’t also advance the next line
                    yield return WaitForKeyRelease(advanceKey);

                    // 3) Move to next line immediately
                    i++;
                    if (i >= lines.Count) break;
                    bubble.TypeOut(lines[i]);
                }
                else
                {
                    // Already fully shown → go to next line
                    yield return WaitForKeyRelease(advanceKey); // debounce
                    i++;
                    if (i >= lines.Count) break;
                    bubble.TypeOut(lines[i]);
                }
            }

            // Optionally: if you want a “press nothing to continue” after typing, omit this and rely solely on E.
            // Otherwise, we just wait for input.
            yield return null;
        }

        EndBlockGameplay();
        runRoutine = null;

        // Optionally hide bubble at the end:
        // bubble.gameObject.SetActive(false);
    }

    
    // void Update()
    // {
    //     if (advanceOnKey && bubble && Input.GetKeyDown(advanceKey))
    //     {
    //         bubble.SetInstant(bubble.GetComponentInChildren<TMPro.TMP_Text>().text);
    //     }
    // }

    // ===== Gameplay blocking =====

    void BeginBlockGameplay()
    {
        // Cache & disable listed components
        cachedStates.Clear();
        foreach (var b in disableDuringMonologue)
        {
            if (!b) continue;
            cachedStates.Add(new BehaviourState { comp = b, wasEnabled = b.enabled });
            b.enabled = false;
        }

        if (playerMovement)
        {
            prevCanMove = playerMovement.IsMovementEnabled;
            prevCanLook = playerMovement.IsLookEnabled;

            playerMovement.SetLookEnabled(true);    // keep looking ON
            playerMovement.SetMovementEnabled(false); // movement OFF
        }

        // Cursor handling
        prevCursorVisible = Cursor.visible;
        prevCursorLock = Cursor.lockState;
        if (showCursorDuringMonologue)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void EndBlockGameplay()
    {
        // Restore components
        foreach (var s in cachedStates)
        {
            if (s.comp) s.comp.enabled = s.wasEnabled;
        }
        cachedStates.Clear();

        if (playerMovement)
        {
            playerMovement.SetLookEnabled(prevCanLook);
            playerMovement.SetMovementEnabled(prevCanMove);
        }

        // Restore cursor
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevCursorLock;
    }
}
