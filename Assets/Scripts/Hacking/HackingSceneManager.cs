using UnityEngine;
using UnityEngine.SceneManagement;

// Updated to support second attempts with new map generation
public class HackingSceneManager : MonoBehaviour
{
    public static HackingSceneManager Instance { get; private set; }
    
    // Static field to persist hack completion status across scenes
    public static bool LastHackWasSuccessful { get; private set; }
    
    [Header("Testosterone Settings")]
    [Tooltip("Multiplier for testosterone depletion during hacking (e.g., 2.0 = twice as fast, 0.5 = half speed, 0 = no depletion)")]
    public float testosteroneDepletionMultiplier = 2.0f;
    
    [Header("Scene Management")]
    [Tooltip("Scene to return to after hacking is complete")]
    public string returnSceneName = "Main";
    [Tooltip("Check every X seconds if all tiles are hacked")]
    public float checkCompletionInterval = 0.5f;
    
    [Header("Attempt Settings")]
    [Tooltip("Maximum number of attempts allowed (e.g., 3 = player gets 3 tries)")]
    public int maxAttempts = 3;
    
    [Header("UI Messages")]
    [Tooltip("Text to display attempts remaining (optional)")]
    public TMPro.TextMeshProUGUI attemptsRemainingText;
    [TextArea(2, 4)]
    [Tooltip("Message to display when player loses")]
    public string lossMessage = "YOU LOST!\n\nAll attempts exhausted.";
    [TextArea(2, 4)]
    [Tooltip("Message to display when player wins")]
    public string winMessage = "YOU WIN!\n\nSocial media deleted successfully.";
    
    [Header("References")]
    public GridMaze gridMaze;
    
    private float _originalDecayRate;
    private bool _hasModifiedDecay;
    private float _checkTimer;
    private bool _isInHackingScene;
    private string _currentSceneName;
    private int _attemptCount;
    private bool _isHandlingTestosteroneDepletion;
    private GameObject _lossButton;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Reset hack status when entering scene
        LastHackWasSuccessful = false;
        _attemptCount = 0;
        _isHandlingTestosteroneDepletion = false;
        
        _currentSceneName = SceneManager.GetActiveScene().name;
    }
    
    void Start()
    {
        CheckCurrentScene();
        
        // Subscribe to testosterone depletion event
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            testSystem.OnDepleted.AddListener(HandleTestosteroneDepletion);
        }
    }
    
    void Update()
    {
        // Auto-find GridMaze if we're in a scene but don't have it yet
        if (_isInHackingScene && gridMaze == null)
        {
            TryFindGridMaze();
        }
        
        // Check periodically if all hackable tiles are completed
        if (_isInHackingScene && gridMaze != null)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= checkCompletionInterval)
            {
                _checkTimer = 0f;
                CheckHackingCompletion();
            }
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentSceneName = scene.name;
        Debug.Log($"[HackingSceneManager] Scene loaded: {scene.name}");
        
        // Clear GridMaze reference when changing scenes
        gridMaze = null;
        _checkTimer = 0f;
        
        CheckCurrentScene();
    }
    
    void CheckCurrentScene()
    {
        // Determine if we're in the hacking scene
        _isInHackingScene = !string.IsNullOrEmpty(_currentSceneName) && 
                           !_currentSceneName.Equals(returnSceneName, System.StringComparison.OrdinalIgnoreCase);
        
        if (_isInHackingScene)
        {
            Debug.Log($"[HackingSceneManager] Entered hacking scene '{_currentSceneName}' - looking for GridMaze...");
            // Reset attempt tracking for new hacking session
            _attemptCount = 0;
            _isHandlingTestosteroneDepletion = false;
            TryFindGridMaze();
            FindAndHideLossButton();
            ApplyTestosteroneMultiplier();
            UpdateAttemptsDisplay();
        }
        else
        {
            Debug.Log($"[HackingSceneManager] In main scene '{_currentSceneName}' - restoring normal testosterone rate");
            RestoreOriginalDecayRate();
            HideAttemptsDisplay();
            _lossButton = null; // Clear reference when leaving hacking scene
        }
    }
    
    void TryFindGridMaze()
    {
        if (gridMaze == null)
        {
            gridMaze = FindFirstObjectByType<GridMaze>();
            if (gridMaze != null)
            {
                Debug.Log("[HackingSceneManager] GridMaze found!");
            }
            else
            {
                Debug.LogWarning("[HackingSceneManager] No GridMaze found in scene yet (may still be loading)");
            }
        }
    }
    
    void FindAndHideLossButton()
    {
        if (_lossButton == null)
        {
            _lossButton = GameObject.FindGameObjectWithTag("ButtonLoss");
            if (_lossButton != null)
            {
                // Register the button click handler
                var button = _lossButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    // Clear any existing listeners and add our function
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(ReturnToMainMenu);
                    Debug.Log("[HackingSceneManager] Loss button found, click handler registered, and button hidden");
                }
                else
                {
                    Debug.LogWarning("[HackingSceneManager] ButtonLoss GameObject found but has no Button component!");
                }
                
                _lossButton.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[HackingSceneManager] No GameObject found with tag 'ButtonLoss'");
            }
        }
        else
        {
            // Just hide it if we already have the reference
            _lossButton.SetActive(false);
        }
    }
    

    
    void OnEnable()
    {
        // Only apply multiplier if we're in the hacking scene
        if (_isInHackingScene)
            ApplyTestosteroneMultiplier();
    }
    
    void OnDisable()
    {
        RestoreOriginalDecayRate();
    }
    
    void OnDestroy()
    {
        RestoreOriginalDecayRate();
        
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
            Instance = null;
    }
    
    void ApplyTestosteroneMultiplier()
    {
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem == null)
        {
            Debug.LogWarning("[HackingSceneManager] TestosteroneSystem not found!");
            return;
        }
        
        // Get current decay rate using reflection
        var decayField = testSystem.GetType().GetField("decayPerSecond", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (decayField != null)
        {
            if (!_hasModifiedDecay)
            {
                _originalDecayRate = (float)decayField.GetValue(testSystem);
            }
            
            float newDecayRate = _originalDecayRate * testosteroneDepletionMultiplier;
            
            testSystem.SetDecay(newDecayRate);
            _hasModifiedDecay = true;
            
            Debug.Log($"[HackingSceneManager] Applied testosterone multiplier {testosteroneDepletionMultiplier}x (from {_originalDecayRate:F2} to {newDecayRate:F2} per second)");
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] Could not access decayPerSecond field!");
        }
    }
    
    void RestoreOriginalDecayRate()
    {
        if (!_hasModifiedDecay) return;
        
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            testSystem.SetDecay(_originalDecayRate);
            Debug.Log($"[HackingSceneManager] Restored original testosterone decay rate: {_originalDecayRate:F2}");
        }
        
        _hasModifiedDecay = false;
    }
    
    [ContextMenu("Test Apply Multiplier")]
    void TestApplyMultiplier()
    {
        ApplyTestosteroneMultiplier();
    }
    
    [ContextMenu("Test Restore Original")]
    void TestRestoreOriginal()
    {
        RestoreOriginalDecayRate();
    }
    
    void CheckHackingCompletion()
    {
        if (gridMaze == null) return;
        
        int hackedCount = gridMaze.GetHackedTilesCount();
        int totalHackable = gridMaze.GetTotalHackableTiles();
        
        // Check if all hackable tiles have been hacked
        if (hackedCount >= totalHackable && totalHackable > 0)
        {
            Debug.Log($"[HackingSceneManager] All tiles hacked! ({hackedCount}/{totalHackable}) - Showing success message...");
            LastHackWasSuccessful = true;
            
            // Stop checking for completion
            _checkTimer = -999f;
            
            // Show success message and return after delay
            StartCoroutine(ShowSuccessAndReturn());
        }
    }
    
    System.Collections.IEnumerator ShowSuccessAndReturn()
    {
        Debug.Log("[HackingSceneManager] All tiles hacked - showing win UI...");
        
        // Stop checking for completion
        _checkTimer = -999f;
        
        // Brief pause
        yield return new WaitForSeconds(0.5f);
        
        // Update attempts text to show win message
        if (attemptsRemainingText != null)
        {
            attemptsRemainingText.text = winMessage;
            attemptsRemainingText.gameObject.SetActive(true);
        }
        
        // Enable the same button used for losing (ButtonLoss)
        if (_lossButton == null)
        {
            FindAndHideLossButton(); // This will cache it
        }
        
        if (_lossButton != null)
        {
            _lossButton.SetActive(true);
            Debug.Log("[HackingSceneManager] Button enabled (showing win message)");
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] Button with tag 'ButtonLoss' not found!");
        }
        
        // Show and unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("[HackingSceneManager] Cursor enabled and unlocked");
        
        // Disable player movement
        DisablePlayerMovement();
    }
    
    public void ReturnToMainScene()
    {
        if (string.IsNullOrEmpty(returnSceneName))
        {
            Debug.LogError("[HackingSceneManager] Return scene name is not set!");
            return;
        }

        // Restore before changing scenes
        RestoreOriginalDecayRate();
        
        // Reset state flags
        _isHandlingTestosteroneDepletion = false;

        Debug.Log($"[HackingSceneManager] Returning to scene '{returnSceneName}' - Hack successful: {LastHackWasSuccessful}");
        SceneManager.LoadScene(returnSceneName);
    }
    
    /// <summary>
    /// Call this to manually fail and return to main scene
    /// </summary>
    public void FailAndReturn()
    {
        LastHackWasSuccessful = false;
        ReturnToMainScene();
    }
    
    /// <summary>
    /// Check the result of the last hack from the main scene
    /// </summary>
    public static bool WasLastHackSuccessful()
    {
        return LastHackWasSuccessful;
    }
    
    /// <summary>
    /// Reset the hack status (call this after you've checked the result)
    /// </summary>
    public static void ResetHackStatus()
    {
        LastHackWasSuccessful = false;
    }
    
    void HandleTestosteroneDepletion()
    {
        // Prevent multiple calls
        if (_isHandlingTestosteroneDepletion) return;
        
        // Only handle if we're in the hacking scene
        if (!_isInHackingScene) return;
        
        _isHandlingTestosteroneDepletion = true;
        _attemptCount++;
        
        Debug.Log($"[HackingSceneManager] Testosterone depleted! Attempt {_attemptCount} of {maxAttempts}");
        
        // Update attempts display
        UpdateAttemptsDisplay();
        
        // Check if more attempts are available
        if (_attemptCount < maxAttempts)
        {
            Debug.Log($"[HackingSceneManager] Giving player attempt {_attemptCount + 1}...");
            StartCoroutine(StartNextAttempt());
        }
        else
        {
            Debug.Log("[HackingSceneManager] No more attempts - failing hack and applying failure actions");
            // Out of attempts, fail the hack and handle failure
            LastHackWasSuccessful = false;
            StartCoroutine(HandleAllAttemptsFailed());
        }
    }
    
    System.Collections.IEnumerator StartNextAttempt()
    {
        // Brief pause
        yield return new WaitForSeconds(0.5f);
        
        // Refill testosterone
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            testSystem.ResetToStart();
            Debug.Log($"[HackingSceneManager] Testosterone refilled for attempt {_attemptCount + 1}");
        }
        
        // Generate new map
        if (gridMaze != null)
        {
            gridMaze.RegenerateMap();
            Debug.Log($"[HackingSceneManager] New map generated for attempt {_attemptCount + 1}");
            
            // Reset player to start position after map regeneration
            var player = FindFirstObjectByType<GridBallPlayer>();
            if (player != null)
            {
                player.ResetToMazeStart();
                Debug.Log($"[HackingSceneManager] Player reset to maze start");
            }
            else
            {
                Debug.LogWarning("[HackingSceneManager] GridBallPlayer not found - cannot reset player position");
            }
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] GridMaze not found - cannot regenerate map");
        }
        
        // Reset flag
        _isHandlingTestosteroneDepletion = false;
    }
    
    System.Collections.IEnumerator HandleAllAttemptsFailed()
    {
        Debug.Log("[HackingSceneManager] All attempts exhausted - showing loss UI...");
        
        // Brief pause
        yield return new WaitForSeconds(0.5f);
        
        // Update attempts text to show loss message
        if (attemptsRemainingText != null)
        {
            attemptsRemainingText.text = lossMessage;
            attemptsRemainingText.gameObject.SetActive(true);
        }
        
        // Enable loss button (find it if we don't have it cached)
        if (_lossButton == null)
        {
            FindAndHideLossButton(); // This will cache it
        }
        
        if (_lossButton != null)
        {
            _lossButton.SetActive(true);
            Debug.Log("[HackingSceneManager] Loss button enabled");
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] Loss button with tag 'ButtonLoss' not found!");
        }
        
        // Show and unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("[HackingSceneManager] Cursor enabled and unlocked");
        
        // Disable player movement
        DisablePlayerMovement();
    }
    
    void UpdateAttemptsDisplay()
    {
        if (attemptsRemainingText == null) return;
        
        int attemptsLeft = maxAttempts - _attemptCount;
        attemptsRemainingText.text = $"Attempts Remaining: {attemptsLeft}";
        attemptsRemainingText.gameObject.SetActive(true);
        
        Debug.Log($"[HackingSceneManager] Updated attempts display: {attemptsLeft} remaining");
    }
    
    void HideAttemptsDisplay()
    {
        if (attemptsRemainingText == null) return;
        
        attemptsRemainingText.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Public function to be called from the main menu button after loss.
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[HackingSceneManager] Returning to main menu...");
        
        // Restore original testosterone decay rate
        RestoreOriginalDecayRate();
        
        // Subscribe to scene loaded event to show menu after main scene loads
        SceneManager.sceneLoaded += OnMenuSceneLoaded;
        
        // Mark as failed
        LastHackWasSuccessful = false;
        
        // Load the main scene
        ReturnToMainScene();
    }
    
    void OnMenuSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe immediately
        SceneManager.sceneLoaded -= OnMenuSceneLoaded;
        
        Debug.Log("[HackingSceneManager] Main scene loaded - showing menu...");
        
        // Use StartMenuController to show the menu
        if (StartMenuController.Instance != null)
        {
            StartMenuController.Instance.ResetToMenu();
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] StartMenuController not found! Cannot show menu.");
        }
    }
    
    void DisablePlayerMovement()
    {
        // Find the player and disable movement components
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Disable common movement scripts
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
                Debug.Log("[HackingSceneManager] PlayerMovement disabled");
            }
            
            // Disable CharacterController if present
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                Debug.Log("[HackingSceneManager] CharacterController disabled");
            }
            
            // Disable Rigidbody if present
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                Debug.Log("[HackingSceneManager] Rigidbody set to kinematic");
            }
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] Player not found to disable movement!");
        }
    }
}
