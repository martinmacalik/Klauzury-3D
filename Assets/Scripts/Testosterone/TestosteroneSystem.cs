using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TestosteroneSystem : MonoBehaviour
{
    public static TestosteroneSystem Instance { get; private set; }

    [Header("Tuning")]
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float startValue = 100f;
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
        // Check if we're currently IN the hacking scene
        // HackingSceneManager persists across scenes, so we need to check the actual scene
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isInHackingScene = HackingSceneManager.Instance != null && 
                                !currentScene.Equals(HackingSceneManager.Instance.returnSceneName, System.StringComparison.OrdinalIgnoreCase);
        
        if (isInHackingScene)
        {
            Debug.Log($"[TestosteroneSystem] Currently in hacking scene '{currentScene}' - HackingSceneManager will handle depletion");
            yield break;
        }

        Debug.Log($"[TestosteroneSystem] Testosterone depleted in main scene '{currentScene}' - showing game over");

        // Show "You Lost" message
        if (gameOverText != null)
        {
            gameOverText.text = gameOverMessage;
            gameOverText.gameObject.SetActive(true);
            Debug.Log("[TestosteroneSystem] Game over text displayed");

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
        else
        {
            Debug.LogWarning("[TestosteroneSystem] Game over text not assigned! Waiting 2 seconds before menu...");
            // Wait a bit even if no UI is assigned
            yield return new WaitForSeconds(2f);
        }

        // Try to use StartMenuController
        if (StartMenuController.Instance != null)
        {
            Debug.Log("[TestosteroneSystem] Calling StartMenuController.ResetToMenu()");
            StartMenuController.Instance.ResetToMenu();
        }
        else
        {
            // Fallback: directly load the menu scene
            Debug.LogWarning("[TestosteroneSystem] No StartMenuController found! Loading menu scene directly...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(0); // Load first scene (usually menu)
        }
    }
}
