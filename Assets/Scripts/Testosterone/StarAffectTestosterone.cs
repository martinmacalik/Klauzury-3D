using UnityEngine;

public class StarAffectsTestosterone : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMenuController menu;        // if null, auto-grab singleton
    public TestosteroneSystem system;        // if null, auto-grab singleton

    [Header("Max Capacity Growth")]
    [Tooltip("How much extra MAX capacity each star adds, as a percent of the original max.")]
    [Range(0f, 1f)] public float maxPercentPerStar = 0.12f; // 12% more max per star

    [Header("Depletion Slowdown")]
    [Tooltip("Multiply decay by this per star. 0.9 = 10% slower per star.")]
    [Range(0.5f, 1f)] public float decayMultiplierPerStar = 0.9f;

    [Header("Base Values (auto-read where possible)")]
    [Tooltip("If TestosteroneSystem doesn’t expose a getter, set this to match its initial Decay/sec in the Inspector.")]
    public float baseDecayPerSecond = 2f;   // fallback if we can’t read from system
    private float baseMax;

    private int _lastAppliedStars = -1;

    void Awake()
    {
        if (!menu)   menu   = PlayerMenuController.Instance;  // your existing singleton :contentReference[oaicite:0]{index=0}
        if (!system) system = TestosteroneSystem.Instance;     // your existing singleton :contentReference[oaicite:1]{index=1}
        if (!system) { Debug.LogWarning("[StarAffectsT] No TestosteroneSystem found."); return; }

        baseMax = system.Max;                                  // read original max from system :contentReference[oaicite:2]{index=2}
        // If you add a Decay getter to TestosteroneSystem, read it here (see note below)
        ApplyNow();
    }

    void Update()
    {
        // Cheap + reliable: re-apply if star level changed
        if (!menu || !system) return;
        if (_lastAppliedStars != menu.StarLevel)
            ApplyNow();
    }

    public void ApplyNow()
    {
        if (!menu || !system) return;

        int stars = Mathf.Max(0, menu.StarLevel);
        _lastAppliedStars = stars;

        // 1) Capacity boost: baseMax * (1 + maxPct*stars)
        float newMax = baseMax * (1f + maxPercentPerStar * stars);
        system.SetMax(newMax, keepPercent: true);              // keeps current % filled the same :contentReference[oaicite:3]{index=3}

        // 2) Slowdown: baseDecay * (decayMult^stars)
        float newDecay = baseDecayPerSecond * Mathf.Pow(decayMultiplierPerStar, stars);
        system.SetDecay(newDecay);                              // updates internal decay per second :contentReference[oaicite:4]{index=4}

        // Debug
        // Debug.Log($"[StarAffectsT] stars={stars} → Max={newMax:F1}, Decay/s={newDecay:F3}");
    }
}
