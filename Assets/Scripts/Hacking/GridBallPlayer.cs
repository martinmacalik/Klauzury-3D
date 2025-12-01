using UnityEngine;
using System.Collections;

public class GridBallPlayer : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Size of one grid cell")]
    public float gridSize = 1f;
    
    [Header("Movement Settings")]
    [Tooltip("Time it takes to move from one cell to another")]
    public float moveSpeed = 0.2f;
    
    [Tooltip("Animation curve for smooth movement")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Input Settings")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    
    [Header("Visual Feedback")]
    [Tooltip("Optional: Ball will rotate when moving")]
    public bool rotateWhenMoving = true;
    
    [Tooltip("How much the ball rotates per unit moved")]
    public float rotationSpeed = 360f;
    
    [Header("Camera Follow")]
    [Tooltip("Camera that should follow the player")]
    public Camera followCamera;
    
    [Tooltip("Offset from player position")]
    public Vector3 cameraOffset = new Vector3(0, 10, -10);
    
    [Tooltip("How smoothly camera follows")]
    public float cameraFollowSpeed = 5f;
    
    [Header("Maze Integration")]
    [Tooltip("Reference to the GridMaze component")]
    public GridMaze gridMaze;
    
    [Tooltip("Auto-find GridMaze in scene")]
    public bool autoFindMaze = true;
    
    [Header("Grid Visualization")]
    [Tooltip("Show grid in game")]
    public bool showGridInGame = false;
    
    [Tooltip("Size of the grid to display (cells in each direction from player)")]
    public int gridDisplayRadius = 10;
    
    [Tooltip("Material for grid lines")]
    public Material gridLineMaterial;
    
    [Tooltip("Color of grid lines")]
    public Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Tooltip("Height offset for grid (so it doesn't z-fight with ground)")]
    public float gridHeightOffset = -0.5f;
    
    // Internal state
    private Vector3 _targetPosition;
    private bool _isMoving = false;
    private Vector3 _currentGridPosition;
    private GameObject _gridContainer;
    private LineRenderer[] _gridLines;
    
    void Start()
    {
        Debug.Log("[GridBallPlayer] Starting initialization...");
        Debug.Log($"[GridBallPlayer] GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"[GridBallPlayer] Component enabled: {enabled}");
        
        // Find maze if auto-find is enabled
        if (autoFindMaze && gridMaze == null)
        {
            gridMaze = FindFirstObjectByType<GridMaze>();
            if (gridMaze != null)
            {
                Debug.Log("[GridBallPlayer] GridMaze auto-found!");
            }
            else
            {
                Debug.LogWarning("[GridBallPlayer] No GridMaze found in scene!");
            }
        }
        
        // Check for Rigidbody that might interfere
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.LogWarning($"[GridBallPlayer] Rigidbody detected! isKinematic={rb.isKinematic}. Setting to kinematic to prevent physics interference.");
            rb.isKinematic = true; // Make kinematic so physics doesn't interfere with our movement
        }
        
        // If maze exists, start at a valid position
        if (gridMaze != null)
        {
            Vector3 startPos = gridMaze.GetStartPosition();
            transform.position = startPos;
            Debug.Log($"[GridBallPlayer] Starting at maze start position: {startPos}");
        }
        else
        {
            // Snap to nearest grid position on start
            SnapToGrid();
        }
        
        _targetPosition = transform.position;
        _currentGridPosition = GetGridPosition(transform.position);
        
        Debug.Log($"[GridBallPlayer] Initial grid position: {_currentGridPosition}, world position: {transform.position}");
        Debug.Log($"[GridBallPlayer] _isMoving = {_isMoving}");
        Debug.Log($"[GridBallPlayer] Grid size: {gridSize}, Move speed: {moveSpeed}");
        
        // Find camera if not assigned
        if (!followCamera)
        {
            followCamera = Camera.main;
            if (followCamera)
            {
                Debug.Log("[GridBallPlayer] Camera auto-found");
            }
            else
            {
                Debug.LogError("[GridBallPlayer] No camera found!");
            }
        }
        
        // Position camera initially
        if (followCamera)
        {
            followCamera.transform.position = transform.position + cameraOffset;
            followCamera.transform.LookAt(transform.position);
            Debug.Log($"[GridBallPlayer] Camera positioned at {followCamera.transform.position}");
        }
        
        // Create grid visualization
        if (showGridInGame)
        {
            CreateGridVisualization();
            Debug.Log("[GridBallPlayer] Grid visualization created");
        }
        
        Debug.Log("[GridBallPlayer] Initialization complete! Ready for input.");
    }
    
    void Update()
    {
        if (!_isMoving)
        {
            HandleInput();
        }
        else
        {
            // Debug: Show when movement is in progress
            if (Input.anyKeyDown)
            {
                Debug.Log("[GridBallPlayer] Currently moving, input blocked");
            }
        }
    }
    
    void LateUpdate()
    {
        // Camera follow - maintains the same offset angle
        if (followCamera)
        {
            Vector3 targetPosition = transform.position + cameraOffset;
            followCamera.transform.position = Vector3.Lerp(
                followCamera.transform.position,
                targetPosition,
                Time.deltaTime * cameraFollowSpeed
            );
            
            // Keep looking at the player
            followCamera.transform.LookAt(transform.position);
        }
    }
    
    void HandleInput()
    {
        // Debug: Check if ANY input is being detected
        if (Input.anyKeyDown)
        {
            Debug.Log("[GridBallPlayer] Some key was pressed!");
        }
        
        Vector3 moveDirection = Vector3.zero;
        
        // Check WASD keys with both GetKeyDown (for initial press) and GetKey (for held keys)
        if (Input.GetKeyDown(moveUpKey) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("[GridBallPlayer] Move UP detected");
            moveDirection = Vector3.back;  // Flipped
        }
        else if (Input.GetKeyDown(moveDownKey) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("[GridBallPlayer] Move DOWN detected");
            moveDirection = Vector3.forward;  // Flipped
        }
        else if (Input.GetKeyDown(moveLeftKey) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("[GridBallPlayer] Move LEFT detected");
            moveDirection = Vector3.right;  // Flipped
        }
        else if (Input.GetKeyDown(moveRightKey) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("[GridBallPlayer] Move RIGHT detected");
            moveDirection = Vector3.left;  // Flipped
        }
        
        // If a direction was pressed, try to move
        if (moveDirection != Vector3.zero)
        {
            Debug.Log($"[GridBallPlayer] Attempting to move in direction: {moveDirection}");
            TryMove(moveDirection);
        }
    }
    
    void TryMove(Vector3 direction)
    {
        Vector3 targetGridPos = _currentGridPosition + direction;
        Vector3 targetWorldPos = GridToWorld(targetGridPos);
        
        Debug.Log($"[GridBallPlayer] TryMove - Current: {transform.position}, Target Grid: {targetGridPos}, Target World: {targetWorldPos}");
        
        // Check if the target position is valid (you can add collision checks here)
        if (CanMoveTo(targetWorldPos))
        {
            Debug.Log($"[GridBallPlayer] Starting coroutine to move from {transform.position} to {targetWorldPos}");
            StartCoroutine(MoveToPosition(targetWorldPos, direction));
            _currentGridPosition = targetGridPos;
        }
        else
        {
            Debug.Log("[GridBallPlayer] Cannot move to that position - blocked!");
        }
    }
    
    bool CanMoveTo(Vector3 worldPosition)
    {
        // Check against maze if available
        if (gridMaze != null)
        {
            bool walkable = gridMaze.IsWalkable(worldPosition);
            if (!walkable)
            {
                Debug.Log($"[GridBallPlayer] Position {worldPosition} is not walkable in maze");
            }
            return walkable;
        }
        
        // Fallback: check for physical obstacles
        Collider[] colliders = Physics.OverlapSphere(worldPosition, gridSize * 0.4f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Wall"))
            {
                Debug.Log($"[GridBallPlayer] Wall detected at {worldPosition}");
                return false;
            }
        }
        
        return true;
    }
    
    IEnumerator MoveToPosition(Vector3 targetPos, Vector3 direction)
    {
        Debug.Log($"[GridBallPlayer] MoveToPosition coroutine started! Target: {targetPos}");
        _isMoving = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        
        Debug.Log($"[GridBallPlayer] Start position: {startPos}, Move speed: {moveSpeed}");
        
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = transform.rotation;
        
        if (rotateWhenMoving)
        {
            // Calculate rotation axis perpendicular to movement direction
            Vector3 rotationAxis = Vector3.Cross(direction, Vector3.up);
            targetRotation = Quaternion.AngleAxis(rotationSpeed * gridSize, rotationAxis) * startRotation;
        }
        
        int frameCount = 0;
        while (elapsed < moveSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveSpeed;
            float curveValue = moveCurve.Evaluate(t);
            
            // Move position
            transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
            
            // Rotate ball
            if (rotateWhenMoving)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            }
            
            frameCount++;
            if (frameCount % 10 == 0) // Log every 10 frames
            {
                Debug.Log($"[GridBallPlayer] Moving... t={t:F2}, pos={transform.position}");
            }
            
            yield return null;
        }
        
        // Ensure we end exactly at target
        transform.position = targetPos;
        if (rotateWhenMoving)
        {
            transform.rotation = targetRotation;
        }
        
        Debug.Log($"[GridBallPlayer] Movement complete! Final position: {transform.position}");
        _isMoving = false;
    }
    
    void SnapToGrid()
    {
        Vector3 gridPos = GetGridPosition(transform.position);
        transform.position = GridToWorld(gridPos);
    }
    
    Vector3 GetGridPosition(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / gridSize),
            Mathf.Round(worldPosition.y / gridSize),
            Mathf.Round(worldPosition.z / gridSize)
        );
    }
    
    Vector3 GridToWorld(Vector3 gridPosition)
    {
        return new Vector3(
            gridPosition.x * gridSize,
            gridPosition.y * gridSize,
            gridPosition.z * gridSize
        );
    }
    
    // Public method to get current grid position
    public Vector3 GetCurrentGridPosition()
    {
        return _currentGridPosition;
    }
    
    // Public method to teleport to a specific grid position
    public void TeleportToGrid(Vector3 gridPosition)
    {
        _currentGridPosition = gridPosition;
        transform.position = GridToWorld(gridPosition);
        _targetPosition = transform.position;
    }
    
    // Public method to reset player to maze start position
    public void ResetToMazeStart()
    {
        if (gridMaze != null)
        {
            Vector3 startPos = gridMaze.GetStartPosition();
            transform.position = startPos;
            _currentGridPosition = GetGridPosition(startPos);
            _targetPosition = startPos;
            _isMoving = false;
            Debug.Log($"[GridBallPlayer] Reset to maze start position: {startPos}");
        }
        else
        {
            Debug.LogWarning("[GridBallPlayer] Cannot reset - no GridMaze reference!");
        }
    }
    
    void CreateGridVisualization()
    {
        // Create a container for grid lines
        _gridContainer = new GameObject("GridVisualization");
        _gridContainer.transform.parent = transform;
        _gridContainer.transform.localPosition = Vector3.zero;
        
        int linesCount = (gridDisplayRadius * 2 + 1) * 2; // Horizontal + Vertical lines
        _gridLines = new LineRenderer[linesCount];
        
        int lineIndex = 0;
        
        // Create horizontal lines (along X axis)
        for (int z = -gridDisplayRadius; z <= gridDisplayRadius; z++)
        {
            GameObject lineObj = new GameObject($"GridLine_H_{z}");
            lineObj.transform.parent = _gridContainer.transform;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            SetupLineRenderer(lr);
            
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-gridDisplayRadius * gridSize, gridHeightOffset, z * gridSize));
            lr.SetPosition(1, new Vector3(gridDisplayRadius * gridSize, gridHeightOffset, z * gridSize));
            
            _gridLines[lineIndex++] = lr;
        }
        
        // Create vertical lines (along Z axis)
        for (int x = -gridDisplayRadius; x <= gridDisplayRadius; x++)
        {
            GameObject lineObj = new GameObject($"GridLine_V_{x}");
            lineObj.transform.parent = _gridContainer.transform;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            SetupLineRenderer(lr);
            
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x * gridSize, gridHeightOffset, -gridDisplayRadius * gridSize));
            lr.SetPosition(1, new Vector3(x * gridSize, gridHeightOffset, gridDisplayRadius * gridSize));
            
            _gridLines[lineIndex++] = lr;
        }
    }
    
    void SetupLineRenderer(LineRenderer lr)
    {
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.useWorldSpace = false;
        
        if (gridLineMaterial != null)
        {
            lr.material = gridLineMaterial;
        }
        else
        {
            // Create a simple unlit material if none provided
            Material defaultMat = new Material(Shader.Find("Unlit/Color"));
            defaultMat.color = gridLineColor;
            lr.material = defaultMat;
        }
        
        lr.startColor = gridLineColor;
        lr.endColor = gridLineColor;
    }
    

    void OnDestroy()
    {
        // Clean up grid visualization
        if (_gridContainer != null)
        {
            Destroy(_gridContainer);
        }
    }
    
    // Visualize the grid in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 currentGrid = _currentGridPosition;
        if (!Application.isPlaying)
        {
            currentGrid = GetGridPosition(transform.position);
        }
        
        // Draw current grid cell
        Vector3 worldPos = GridToWorld(currentGrid);
        Gizmos.DrawWireCube(worldPos, Vector3.one * gridSize * 0.9f);
        
        // Draw adjacent cells
        Gizmos.color = Color.cyan * 0.5f;
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var dir in directions)
        {
            Vector3 adjacentPos = GridToWorld(currentGrid + dir);
            Gizmos.DrawWireCube(adjacentPos, Vector3.one * gridSize * 0.8f);
        }
    }
}

