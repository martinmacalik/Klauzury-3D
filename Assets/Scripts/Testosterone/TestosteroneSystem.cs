using System;
using UnityEngine;
using UnityEngine.Events;

public class TestosteroneSystem : MonoBehaviour
{
    public static TestosteroneSystem Instance { get; private set; }

    [Header("Tuning")]
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float startValue = 60f;
    [SerializeField] private float decayPerSecond = 2f;

    [Header("Events")]
    public UnityEvent OnDepleted;
    public UnityEvent<float> OnValueChanged; // normalized [0..1]

    public float Current { get; private set; }
    public float Max => maxValue;
    public float Normalized => maxValue <= 0 ? 0f : Current / maxValue;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // keep the original
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Current = Mathf.Clamp(startValue, 0f, maxValue);
        OnValueChanged?.Invoke(Normalized);
    }

    void Update()
    {
        if (Current <= 0f) return;

        float old = Current;
        Current = Mathf.Max(0f, Current - decayPerSecond * Time.deltaTime);

        if (!Mathf.Approximately(old, Current))
        {
            OnValueChanged?.Invoke(Normalized);
            if (Current <= 0f) OnDepleted?.Invoke();
        }
    }

    public void Gain(float amount)
    {
        if (amount <= 0f) return;
        float old = Current;
        Current = Mathf.Min(maxValue, Current + amount);
        if (!Mathf.Approximately(old, Current))
            OnValueChanged?.Invoke(Normalized);
    }

    // Utility setters to tweak at runtime / from menus if you want
    public void SetMax(float newMax, bool keepPercent = true)
    {
        newMax = Mathf.Max(1f, newMax);
        float pct = keepPercent && maxValue > 0f ? Current / maxValue : Current / newMax;
        maxValue = newMax;
        Current = Mathf.Clamp(pct * maxValue, 0f, maxValue);
        OnValueChanged?.Invoke(Normalized);
    }

    public void SetDecay(float newDecayPerSecond)
    {
        decayPerSecond = Mathf.Max(0f, newDecayPerSecond);
    }

    public void ResetToStart()
    {
        Current = Mathf.Clamp(startValue, 0f, maxValue);
        OnValueChanged?.Invoke(Normalized);
    }
}
