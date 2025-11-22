using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TestosteroneSystem : MonoBehaviour
{
    public static TestosteroneSystem Instance { get; private set; }

    [Header("Tuning")]
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float startValue = 60f;
    [SerializeField] private float decayPerSecond = 0.5f;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private GameObject gameOverBackground; // Optional background panel/image
    [TextArea(2, 4)]
    [SerializeField] private string gameOverMessage = "The testosterone level has been depleted!";
    [SerializeField] private float gameOverDisplayDuration = 3f;

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Don't decay testosterone if player is in a car
        if (IsPlayerInCar())
            return;

        if (Current <= 0f) return;

        float old = Current;
        Current = Mathf.Max(0f, Current - decayPerSecond * Time.deltaTime);

        if (!Mathf.Approximately(old, Current))
        {
            OnValueChanged?.Invoke(Normalized);
            if (Current <= 0f)
            {
                OnDepleted?.Invoke();
                OnTestosteroneDepleted();
            }
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

    private bool IsPlayerInCar()
    {
        // Check if there's an active car (player is driving)
        // CarEnterExit.Active is set when player enters a car
        return CarEnterExit.Active != null;
    }

    private void OnTestosteroneDepleted()
    {
        StartCoroutine(ShowGameOverAndReset());
    }

    [ContextMenu("Reset Game Over Message to Default")]
    private void ResetGameOverMessage()
    {
        gameOverMessage = "The testosterone level has been depleted!";
        Debug.Log("Game Over message reset to default: " + gameOverMessage);
    }

    private IEnumerator ShowGameOverAndReset()
    {
        // Show "You Lost" message
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
            gameOverText.gameObject.SetActive(true);

            // Show background if assigned
            if (gameOverBackground != null)
                gameOverBackground.SetActive(true);

            // Wait for specified duration
            yield return new WaitForSeconds(gameOverDisplayDuration);

            // Hide the text
            gameOverText.gameObject.SetActive(false);

            // Hide background
            if (gameOverBackground != null)
                gameOverBackground.SetActive(false);
        }

        // Use the singleton instance to reset to menu
        if (StartMenuController.Instance != null)
        {
            StartMenuController.Instance.ResetToMenu();
        }
        else
        {
            Debug.LogWarning("TestosteroneSystem: No StartMenuController found to reset to menu!");
        }
    }
}
