using UnityEngine;

[DisallowMultipleComponent]
public class AutoCashPickup : MonoBehaviour, IMoneyAmountReceiver
{
    [Header("Value (will be set by MoneyDropper)")]
    [SerializeField, Min(0)] private int amount = 0;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)] private float lifetime = 2.5f; // seconds until it disappears
    [SerializeField, Min(0f)] private float shrinkDuration = 0.25f; // last part of life shrinks out

    [Header("FX (optional)")]
    [SerializeField] private AudioSource sfxOnSpawn;   // plays when spawned
    [SerializeField] private ParticleSystem vfxOnSpawn;

    bool _added;

    void Start()
    {
        // Pay immediately into the menu once
        AddOnce();

        // Optional FX
        if (sfxOnSpawn) sfxOnSpawn.Play();
        if (vfxOnSpawn) vfxOnSpawn.Play();

        // Start vanish routine
        if (shrinkDuration > 0f && shrinkDuration < lifetime)
            StartCoroutine(ShrinkAndDestroy(lifetime - shrinkDuration, shrinkDuration));
        else
            Destroy(gameObject, lifetime);
    }

    System.Collections.IEnumerator ShrinkAndDestroy(float wait, float fade)
    {
        if (wait > 0f) yield return new WaitForSeconds(wait);

        float t = 0f;
        var start = transform.localScale;
        var target = Vector3.zero;
        while (t < fade)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fade);
            transform.localScale = Vector3.Lerp(start, target, k);
            yield return null;
        }
        Destroy(gameObject);
    }

    void AddOnce()
    {
        if (_added) return;
        _added = true;
        // Add to your existing menu counters
        PlayerMenuController.Instance?.AddMoney(amount); // uses your menu script. :contentReference[oaicite:0]{index=0}
    }

    // Called by MoneyDropper right after instantiation
    public void SetAmount(int value)
    {
        amount = Mathf.Max(0, value);
    }
}