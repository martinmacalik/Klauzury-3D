using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TileHackingController : MonoBehaviour
{
    [Header("References")]
    public GridMaze gridMaze;
    public GridBallPlayer player;
    public GameObject hackingUI;
    public Transform arrowContainer;
    public TextMeshProUGUI instructionText;
    
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public float arrowSpacing = 80f;
    public Color correctArrowColor = Color.green;
    public Color wrongArrowColor = Color.red;
    public Color neutralArrowColor = Color.white;
    
    [Tooltip("Name of the child Image component to color (e.g. 'ArrowImage')")]
    public string arrowImageName = "ArrowImage";
    
    [Header("Hacking Settings")]
    public int minArrows = 5;
    public int maxArrows = 9;
    public KeyCode hackKey = KeyCode.E;
    
    private bool _isHacking = false;
    private List<ArrowDirection> _arrowSequence = new List<ArrowDirection>();
    private List<GameObject> _arrowObjects = new List<GameObject>();
    private int _currentArrowIndex = 0;
    private Vector3 _hackingTilePosition;
    
    public enum ArrowDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    void Start()
    {
        if (gridMaze == null)
            gridMaze = FindFirstObjectByType<GridMaze>();
        
        if (player == null)
            player = FindFirstObjectByType<GridBallPlayer>();
        
        if (hackingUI != null)
            hackingUI.SetActive(false);
    }
    
    void Update()
    {
        if (_isHacking)
        {
            HandleHackingInput();
        }
        else
        {
            CheckForHackablePosition();
        }
    }
    
    void CheckForHackablePosition()
    {
        if (Input.GetKeyDown(hackKey) && player != null && gridMaze != null)
        {
            Vector3 playerPos = player.transform.position;
            
            if (gridMaze.IsHackableTile(playerPos))
            {
                StartHacking(playerPos);
            }
        }
    }
    
    void StartHacking(Vector3 tilePosition)
    {
        _isHacking = true;
        _hackingTilePosition = tilePosition;
        _currentArrowIndex = 0;
        
        // Block player movement
        if (player != null)
        {
            player.enabled = false;
            Debug.Log("[TileHacking] Player movement blocked");
        }
        
        // Generate random arrow sequence
        GenerateArrowSequence();
        
        // Show UI
        if (hackingUI != null)
            hackingUI.SetActive(true);
        
        // Create arrow visuals
        CreateArrowVisuals();
        
        // Update instruction
        if (instructionText != null)
            instructionText.text = "Repeat the sequence!";
        
        Debug.Log($"[TileHacking] Started hacking with {_arrowSequence.Count} arrows");
    }
    
    void GenerateArrowSequence()
    {
        _arrowSequence.Clear();
        
        int arrowCount = Random.Range(minArrows, maxArrows + 1);
        
        for (int i = 0; i < arrowCount; i++)
        {
            ArrowDirection randomDir = (ArrowDirection)Random.Range(0, 4);
            _arrowSequence.Add(randomDir);
        }
    }
    
    void CreateArrowVisuals()
    {
        // Clear old arrows
        foreach (var arrow in _arrowObjects)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        _arrowObjects.Clear();
        
        if (arrowContainer == null) return;
        
        if (arrowPrefab == null)
        {
            Debug.LogError("[TileHacking] Arrow Prefab is not assigned!");
            return;
        }
        
        // Create new arrows using prefab
        for (int i = 0; i < _arrowSequence.Count; i++)
        {
            GameObject arrowObj = Instantiate(arrowPrefab, arrowContainer);
            arrowObj.name = $"Arrow_{i}_{_arrowSequence[i]}";
            
            Debug.Log($"[TileHacking] Created arrow {i}: Direction={_arrowSequence[i]}, Prefab={arrowPrefab.name}");
            
            // Color the background Image (find SymbolBG child) - DO THIS FIRST
            Transform bgTransform = arrowObj.transform.Find("SymbolBG");
            if (bgTransform != null)
            {
                Image bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    Color beforeColor = bgImage.color;
                    bgImage.color = neutralArrowColor;
                    Debug.Log($"[TileHacking] Arrow {i}: SymbolBG found! Color changed from {beforeColor} to {bgImage.color} (target: {neutralArrowColor})");
                }
                else
                {
                    Debug.LogWarning($"[TileHacking] Arrow {i}: SymbolBG found but has no Image component!");
                }
            }
            else
            {
                Debug.LogWarning($"[TileHacking] Arrow {i}: No child named 'SymbolBG' found! Children are:");
                for (int j = 0; j < arrowObj.transform.childCount; j++)
                {
                    Debug.Log($"  - Child {j}: {arrowObj.transform.GetChild(j).name}");
                }
            }

            // Position arrow
            RectTransform rectTransform = arrowObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(i * arrowSpacing - (_arrowSequence.Count - 1) * arrowSpacing / 2f, 0);
                
                // Rotate arrow based on direction
                // No rotation = Up, 90 = Right, 180 = Down, 270 = Left
                float rotation = GetArrowRotation(_arrowSequence[i]);
                rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
                Debug.Log($"[TileHacking] Arrow {i} rotated to {rotation} degrees");
            }
            else
            {
                Debug.LogError($"[TileHacking] Arrow {i} has no RectTransform!");
            }
            
            _arrowObjects.Add(arrowObj);
        }
    }
    
    float GetArrowRotation(ArrowDirection direction)
    {
        switch (direction)
        {
            case ArrowDirection.Up: return 0f;
            case ArrowDirection.Right: return -90f;  // Negative because UI rotation is counterclockwise
            case ArrowDirection.Down: return -180f;
            case ArrowDirection.Left: return -270f;
            default: return 0f;
        }
    }
    
    void HandleHackingInput()
    {
        ArrowDirection? inputDirection = null;
        
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            inputDirection = ArrowDirection.Up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            inputDirection = ArrowDirection.Down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            inputDirection = ArrowDirection.Left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            inputDirection = ArrowDirection.Right;
        
        if (inputDirection.HasValue)
        {
            CheckArrowInput(inputDirection.Value);
        }
    }
    
    void CheckArrowInput(ArrowDirection input)
    {
        if (_currentArrowIndex >= _arrowSequence.Count)
            return;
        
        ArrowDirection expectedArrow = _arrowSequence[_currentArrowIndex];
        
        if (input == expectedArrow)
        {
            // Correct input
            Debug.Log($"[TileHacking] Correct! {_currentArrowIndex + 1}/{_arrowSequence.Count}");
            
            // Highlight arrow as correct
            if (_currentArrowIndex < _arrowObjects.Count)
            {
                SetArrowColor(_arrowObjects[_currentArrowIndex], correctArrowColor);
            }
            
            _currentArrowIndex++;
            
            // Check if sequence complete
            if (_currentArrowIndex >= _arrowSequence.Count)
            {
                CompleteHacking();
            }
        }
        else
        {
            // Wrong input - restart sequence
            Debug.Log($"[TileHacking] Wrong! Expected {expectedArrow}, got {input}. Restarting...");
            
            // Flash current arrow red
            if (_currentArrowIndex < _arrowObjects.Count)
            {
                StartCoroutine(FlashArrowRed(_arrowObjects[_currentArrowIndex]));
            }
            
            RestartSequence();
        }
    }
    
    void SetArrowColor(GameObject arrowObj, Color color)
    {
        // Color the background Image (find SymbolBG child)
        Transform bgTransform = arrowObj.transform.Find("SymbolBG");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                Color beforeColor = bgImage.color;
                bgImage.color = color;
                Debug.Log($"[TileHacking] SetArrowColor: Changed SymbolBG from {beforeColor} to {bgImage.color} (target: {color})");
            }
            else
            {
                Debug.LogError($"[TileHacking] SetArrowColor: SymbolBG found but NO Image component!");
            }
        }
        else
        {
            Debug.LogError($"[TileHacking] SetArrowColor: NO SymbolBG child found on {arrowObj.name}!");
        }
    }
    
    System.Collections.IEnumerator FlashArrowRed(GameObject arrowObj)
    {
        SetArrowColor(arrowObj, wrongArrowColor);
        yield return new WaitForSeconds(0.3f);
        SetArrowColor(arrowObj, neutralArrowColor);
    }
    
    void RestartSequence()
    {
        _currentArrowIndex = 0;
        
        // Reset all arrow colors
        foreach (var arrowObj in _arrowObjects)
        {
            SetArrowColor(arrowObj, neutralArrowColor);
        }
        
        if (instructionText != null)
            instructionText.text = "Try again!";
    }
    
    void CompleteHacking()
    {
        Debug.Log("[TileHacking] Hacking complete!");
        
        // Mark tile as hacked
        if (gridMaze != null)
        {
            gridMaze.MarkTileAsHacked(_hackingTilePosition);
        }
        
        if (instructionText != null)
            instructionText.text = "SUCCESS!";
        
        // Close UI after a delay
        Invoke(nameof(EndHacking), 1f);
    }
    
    void EndHacking()
    {
        _isHacking = false;
        
        // Re-enable player movement
        if (player != null)
        {
            player.enabled = true;
            Debug.Log("[TileHacking] Player movement restored");
        }
        
        if (hackingUI != null)
            hackingUI.SetActive(false);
        
        // Clear arrows
        foreach (var arrow in _arrowObjects)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        _arrowObjects.Clear();
    }
}

