using UnityEngine;

public class CrosshairWhileADS : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator animator;       // weapon/arms Animator
    [SerializeField] CanvasGroup crosshair;   // CanvasGroup on your Crosshair UI

    [Header("Behavior")]
    [SerializeField] bool requireReady = true; // hide during draw
    [SerializeField] float fadeSpeed = 12f;    // 0 = instant

    static readonly int Hash_IsADS   = Animator.StringToHash("IsADS");
    static readonly int Hash_IsReady = Animator.StringToHash("IsReady");

    void OnEnable()
    {
        SetAlpha(0f, instant:true); // start hidden until ADS
    }

    void Update()
    {
        if (!crosshair) return;

        bool isADS   = animator && animator.GetBool(Hash_IsADS);
        bool isReady = !requireReady || (animator && animator.GetBool(Hash_IsReady));

        float target = (isADS && isReady) ? 1f : 0f;

        crosshair.alpha = Mathf.MoveTowards(crosshair.alpha, target, fadeSpeed * Time.deltaTime);
        bool visible = crosshair.alpha > 0.001f;
        crosshair.blocksRaycasts = visible;
        crosshair.interactable  = visible;
    }

    void SetAlpha(float a, bool instant = false)
    {
        if (!crosshair) return;
        crosshair.alpha = instant ? a : crosshair.alpha;
        bool vis = a > 0.001f;
        crosshair.blocksRaycasts = vis;
        crosshair.interactable  = vis;
    }
}