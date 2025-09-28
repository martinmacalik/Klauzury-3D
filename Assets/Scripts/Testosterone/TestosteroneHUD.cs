using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestosteroneHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TestosteroneSystem system; // will fall back to singleton
    [SerializeField] private Slider slider;             // optional (bar)
    [SerializeField] private TMP_Text valueText;        // <-- assign your TMP label here

    [Header("Display")]
    [SerializeField] private bool showAsPercent = true; // true = "63%" | false = "63"
    [SerializeField] private int decimalPlaces = 0;     // 0 = whole numbers
    [SerializeField] private bool driveSliderAsPercent = true; // Slider 0..100 (nice with whole numbers)

    void OnEnable()
    {
        if (system == null) system = TestosteroneSystem.Instance;
        if (system == null) return;

        // Configure slider once
        if (slider != null)
        {
            if (driveSliderAsPercent)
            {
                slider.minValue = 0f;
                slider.maxValue = 100f;
                slider.wholeNumbers = (decimalPlaces == 0);
            }
            else
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
            }
            slider.value = driveSliderAsPercent ? system.Normalized * 100f : system.Normalized;
        }

        // Init label once
        UpdateLabel(system.Normalized);

        // Subscribe
        system.OnValueChanged.AddListener(OnSystemValueChanged);
    }

    void OnDisable()
    {
        if (system != null)
            system.OnValueChanged.RemoveListener(OnSystemValueChanged);
    }

    private void OnSystemValueChanged(float normalized)
    {
        // Slider
        if (slider != null)
            slider.value = driveSliderAsPercent ? normalized * 100f : normalized;

        // Label
        UpdateLabel(normalized);
    }

    private void UpdateLabel(float normalized)
    {
        if (valueText == null || system == null) return;

        string format = "F" + Mathf.Clamp(decimalPlaces, 0, 6);

        if (showAsPercent)
        {
            float pct = normalized * 100f;
            valueText.text = decimalPlaces == 0 ? Mathf.RoundToInt(pct).ToString() + "%" : pct.ToString(format) + "%";
        }
        else
        {
            // show raw current value from the system
            float raw = system.Current;
            valueText.text = decimalPlaces == 0 ? Mathf.RoundToInt(raw).ToString() : raw.ToString(format);
        }
    }
}
