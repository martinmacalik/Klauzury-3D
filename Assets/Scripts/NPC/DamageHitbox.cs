using UnityEngine;

[DisallowMultipleComponent]
public class DamageHitbox : MonoBehaviour
{
    [Tooltip("Damage multiplier applied when THIS collider is hit.")]
    public float multiplier = 2.5f;

    [Tooltip("Fallback: which object actually takes damage (usually the root).")]
    public MonoBehaviour damageTarget; // must implement IDamageable

    IDamageable cached;

    void Awake()
    {
        // If not set in Inspector, find the first IDamageable up the hierarchy.
        cached = damageTarget as IDamageable;
        if (cached == null)
            cached = GetComponentInParent<IDamageable>();
        if (cached == null)
            Debug.LogWarning($"{name}: DamageHitbox has no IDamageable target in parents.");
    }

    public void ApplyDamage(int baseDamage)
    {
        if (cached == null) return;
        int finalDamage = Mathf.RoundToInt(baseDamage * Mathf.Max(0f, multiplier));
        cached.TakeDamage(finalDamage);
    }
}