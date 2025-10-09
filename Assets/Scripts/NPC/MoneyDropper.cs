using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class MoneyDropper : MonoBehaviour
{
    [System.Serializable]
    public struct CashTier
    {
        [Min(0)] public int minAmount;     // inclusive
        [Min(0)] public int maxAmount;     // inclusive
        public GameObject prefab;          // the pile prefab for this range
    }

    [Header("Total payout range")]
    [SerializeField, Min(0)] private int totalMin = 200;
    [SerializeField, Min(0)] private int totalMax = 1000;

    [Header("Probability bias ( >1 skews low, <1 skews high )")]
    [SerializeField, Range(0.1f, 5f)] private float biasPower = 2.5f;

    [Header("Prefabs per range")]
    [Tooltip("Define non-overlapping, sorted ranges like 200–400, 401–600, etc.")]
    public List<CashTier> tiers = new List<CashTier>();

    [Header("Spawn")]
    public Vector3 spawnOffset = new Vector3(0, 0.25f, 0);
    public bool randomYRotation = true;

    bool _dropped;

    /// <summary>Call this exactly once when the NPC dies.</summary>
    public void Drop()
    {
        if (_dropped) return;
        _dropped = true;

        if (totalMax < totalMin)
        {
            int t = totalMax;
            totalMax = totalMin;
            totalMin = t;
        }

        int amount = SampleBiasedAmount(totalMin, totalMax, biasPower);
        var tier = PickTier(amount);
        if (tier.prefab == null)
        {
            Debug.LogWarning($"MoneyDropper: No prefab set for amount {amount}. Nothing spawned.", this);
            return;
        }

        Quaternion rot = randomYRotation ? Quaternion.Euler(0f, Random.value * 360f, 0f) : tier.prefab.transform.rotation;
        var go = Instantiate(tier.prefab, transform.position + spawnOffset, rot);

        // If the prefab has an amount component, set it; otherwise ignore.
        SetAmountIfSupported(go, amount);
    }

    int SampleBiasedAmount(int min, int max, float power)
    {
        float u = Random.value;                 // uniform 0..1
        float skew = Mathf.Pow(u, power);       // skew toward 0 for power>1
        float val = Mathf.Lerp(min, max + 0.999f, skew);
        return Mathf.Clamp(Mathf.FloorToInt(val / 10f) * 10, min, max); // round to nearest 10
    }

    CashTier PickTier(int amount)
    {
        // Find the first tier whose inclusive range contains 'amount'
        for (int i = 0; i < tiers.Count; i++)
        {
            var t = tiers[i];
            if (amount >= t.minAmount && amount <= t.maxAmount)
                return t;
        }

        // Fallback: pick the closest tier by center
        int best = -1;
        int bestDist = int.MaxValue;
        for (int i = 0; i < tiers.Count; i++)
        {
            int center = (tiers[i].minAmount + tiers[i].maxAmount) / 2;
            int dist = Mathf.Abs(amount - center);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best >= 0 ? tiers[best] : default;
    }

    void SetAmountIfSupported(GameObject go, int amount)
    {
        // Option A: a simple component that exposes SetAmount(int)
        var setter = go.GetComponentInChildren<IMoneyAmountReceiver>();
        if (setter != null) { setter.SetAmount(amount); return; }

        // Option B: a known MonoBehaviour named CashPileAmount with a public 'amount' field
        var cash = go.GetComponentInChildren<CashPileAmount>();
        if (cash != null) { cash.amount = amount; }
    }
}

/// <summary>Optional interface your pickup prefab can implement.</summary>
public interface IMoneyAmountReceiver
{
    void SetAmount(int amount);
}

/// <summary>Optional simple amount holder for your pickup prefab.</summary>
public class CashPileAmount : MonoBehaviour
{
    public int amount;
}
