using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // for LayoutRebuilder
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text text;                 // assign or auto-find
    public RectTransform bubbleRoot;      // background/image RectTransform

    [Header("Follow")]
    public Transform followTarget;        // optional; where to stick the bubble in world
    public Vector3 worldOffset = new Vector3(0, 0.25f, 0);
    public float maxScaleDistance = 25f;

    [Header("Typewriter")]
    public float charsPerSecond = 35f;
    public AudioSource typeSfx;
    
    public bool IsTyping => typing != null;

    Coroutine typing;

    void Awake()
    {
        // Auto-wire refs if missing
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        if (!bubbleRoot)
        {
            var img = GetComponent<Image>();
            if (img) bubbleRoot = img.rectTransform;
            else
            {
                var imgChild = GetComponentInChildren<Image>(true);
                if (imgChild) bubbleRoot = imgChild.rectTransform;
                else bubbleRoot = GetComponent<RectTransform>();
            }
        }
    }

    void LateUpdate()
    {
        if (followTarget)
        {
            transform.position = followTarget.position + worldOffset;

            var cam = Camera.main;
            if (cam)
            {
                float d = Vector3.Distance(cam.transform.position, transform.position);
                float s = Mathf.Lerp(1.25f, 0.8f, Mathf.Clamp01(d / maxScaleDistance));
                transform.localScale = Vector3.one * s;

                // Simple billboard (face camera)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }
    }

    public void SetInstant(string msg)
    {
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null; // important: clear typing handle so IsTyping becomes false
        }
        if (!text) return;
        text.text = msg ?? "";
        text.maxVisibleCharacters = text.text.Length;
        RebuildLayout();
    }

    
    public void RevealAll()
    {
        if (!text) return;
        SetInstant(text.text);
    }


    public Coroutine TypeOut(string msg)
    {
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeRoutine(msg ?? ""));
        return typing;
    }

    IEnumerator TypeRoutine(string msg)
    {
        if (!text) yield break;

        text.text = msg;
        text.maxVisibleCharacters = 0;
        RebuildLayout();

        int shown = 0;
        float step = 1f / Mathf.Max(1f, charsPerSecond);
        float acc = 0f;

        while (shown < msg.Length)
        {
            acc += Time.deltaTime;
            while (acc >= step && shown < msg.Length)
            {
                acc -= step;
                shown++;
                text.maxVisibleCharacters = shown;
                if (typeSfx && typeSfx.clip) typeSfx.PlayOneShot(typeSfx.clip);
            }
            yield return null;
        }
        RebuildLayout();
        typing = null;
    }

    void RebuildLayout()
    {
        if (!bubbleRoot) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRoot);
    }
}
