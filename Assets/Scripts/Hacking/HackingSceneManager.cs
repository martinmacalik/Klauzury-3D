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
    [Tooltip("Display success message for this many seconds before teleporting")]
    public float successMessageDuration = 2f;
    
    [Header("Second Attempt")]
    [Tooltip("Allow a second attempt if testosterone runs out")]
    public bool allowSecondAttempt = true;
    
    [Header("Success UI")]
    [Tooltip("Text to display when hack is successful")]
    public TMPro.TextMeshProUGUI successMessageText;
    [Tooltip("Background panel for success message (optional)")]
    public GameObject successMessageBackground;
    [TextArea(2, 4)]
    public string successMessage = "SOCIAL MEDIA DELETED SUCCESSFULLY\n\nYOU WIN!";
    
    [Header("Player Spawn")]
    [Tooltip("Where to spawn the player when returning to main scene")]
    public Vector3 returnSpawnPosition = Vector3.zero;
    [Tooltip("Player rotation when spawning back")]
    public Vector3 returnSpawnRotation = Vector3.zero;
    
    [Header("References")]
    public GridMaze gridMaze;
    
    // Static fields to store spawn info
    public static Vector3 SpawnPosition { get; private set; }
    public static Vector3 SpawnRotation { get; private set; }
    public static bool ShouldRepositionPlayer { get; private set; }
    
    private float _originalDecayRate;
    private bool _hasModifiedDecay;
    private float _checkTimer;
    private bool _isInHackingScene;
    private string _currentSceneName;
    private int _attemptCount;
    private bool _isHandlingTestosteroneDepletion;
    
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
        
        // Hide success message initially
        if (successMessageText != null)
            successMessageText.gameObject.SetActive(false);
        if (successMessageBackground != null)
            successMessageBackground.SetActive(false);
        
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
            TryFindGridMaze();
            ApplyTestosteroneMultiplier();
        }
        else
        {
            Debug.Log($"[HackingSceneManager] In main scene '{_currentSceneName}' - restoring normal testosterone rate");
            RestoreOriginalDecayRate();
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
        Debug.Log($"[HackingSceneManager] All tiles hacked! Returning to main scene...");
        
        // Store spawn position for main scene
        SpawnPosition = returnSpawnPosition;
        SpawnRotation = returnSpawnRotation;
        ShouldRepositionPlayer = true;
        
        // Subscribe to scene loaded event to show message after scene loads
        SceneManager.sceneLoaded += OnSuccessSceneLoaded;
        
        // Return to main scene immediately (no waiting)
        ReturnToMainScene();
        
        yield break;
    }
    
    void OnSuccessSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only handle if we just successfully hacked
        if (!LastHackWasSuccessful) return;
        
        // Unsubscribe immediately
        SceneManager.sceneLoaded -= OnSuccessSceneLoaded;
        
        Debug.Log("[HackingSceneManager] Main scene loaded after successful hack - displaying message...");
        
        // Start coroutine to display message
        StartCoroutine(DisplaySuccessMessage());
    }
    
    System.Collections.IEnumerator DisplaySuccessMessage()
    {
        // Brief wait to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);
        
        // Find the TMP using PlayerPrompt tag
        GameObject promptObj = GameObject.FindGameObjectWithTag("PlayerPrompt");
        if (promptObj != null)
        {
            TMPro.TextMeshProUGUI tmpText = promptObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = successMessage;
                tmpText.gameObject.SetActive(true);
                Debug.Log("[HackingSceneManager] Success message displayed!");
                
                // Hide after duration
                yield return new WaitForSeconds(successMessageDuration);
                tmpText.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[HackingSceneManager] No TextMeshProUGUI found on PlayerPrompt object!");
            }
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] PlayerPrompt object not found with tag 'PlayerPrompt'!");
        }
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
        
        Debug.Log($"[HackingSceneManager] Testosterone depleted! Attempt count: {_attemptCount}");
        
        // Check if second attempt is allowed
        if (allowSecondAttempt && _attemptCount == 1)
        {
            Debug.Log("[HackingSceneManager] Giving player second attempt...");
            StartCoroutine(StartSecondAttempt());
        }
        else
        {
            Debug.Log("[HackingSceneManager] No more attempts - failing hack");
            // Out of attempts, fail the hack
            LastHackWasSuccessful = false;
            ReturnToMainScene();
        }
    }
    
    System.Collections.IEnumerator StartSecondAttempt()
    {
        // Brief pause
        yield return new WaitForSeconds(0.5f);
        
        // Refill testosterone
        var testSystem = TestosteroneSystem.Instance;
        if (testSystem != null)
        {
            testSystem.ResetToStart();
            Debug.Log("[HackingSceneManager] Testosterone refilled for second attempt");
        }
        
        // Generate new map
        if (gridMaze != null)
        {
            gridMaze.RegenerateMap();
            Debug.Log("[HackingSceneManager] New map generated for second attempt");
        }
        else
        {
            Debug.LogWarning("[HackingSceneManager] GridMaze not found - cannot regenerate map");
        }
        
        // Reset flag
        _isHandlingTestosteroneDepletion = false;
    }
}
