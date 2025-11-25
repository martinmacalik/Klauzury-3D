using UnityEngine;
using System.Collections.Generic;

// Updated to support map regeneration for second attempts
public class GridMaze : MonoBehaviour
{
    public int mazeWidth = 20;
    public int mazeHeight = 20;
    public float cellSize = 1f;
    public int hackableTilesCount = 5;
    
    [Header("Sphere Colors")]
    public Color normalTileColor = Color.green;
    public Color hackableTileColor = Color.blue;
    public Color hackedTileColor = Color.yellow;

    [Header("Connection Lines")]
    public bool showConnectionLines = true;
    public Color lineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public float lineWidth = 0.02f;
    
    private bool[,] _mazeData;
    private GameObject _tilesContainer;
    private GameObject _linesContainer;
    private List<Vector2Int> _hackableTiles = new List<Vector2Int>();
    private HashSet<Vector2Int> _hackedTiles = new HashSet<Vector2Int>();

    void Start() 
    { 
        GenerateMaze(); 
        CreateVisualMaze(); 
    }

    public void GenerateMaze() 
    { 
        // Create grid of VERTICES (where player stands), not cells
        // We need one more vertex than cells in each direction
        _mazeData = new bool[mazeWidth + 1, mazeHeight + 1]; 
        
        // Create random organic shape instead of perfect square
        float centerX = mazeWidth / 2f;
        float centerY = mazeHeight / 2f;
        float maxRadius = Mathf.Min(mazeWidth, mazeHeight) / 2f;
        
        for (int x = 0; x <= mazeWidth; x++) 
        {
            for (int y = 0; y <= mazeHeight; y++) 
            {
                // Calculate distance from center
                float dx = x - centerX;
                float dy = y - centerY;
                float distanceFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Normalize distance (0 = center, 1 = edge)
                float normalizedDistance = distanceFromCenter / maxRadius;
                
                // Add random variation to create organic shape
                float randomVariation = Random.Range(-0.3f, 0.3f);
                float threshold = normalizedDistance + randomVariation;
                
                // Vertices near edge are more likely to be blocked
                if (threshold > 1.2f)
                {
                    // Too far from center, always blocked
                    _mazeData[x, y] = false;
                }
                else if (x >= mazeWidth / 2 - 2 && x <= mazeWidth / 2 + 2 &&
                         y >= mazeHeight / 2 - 2 && y <= mazeHeight / 2 + 2)
                {
                    // Always keep starting position (center area) walkable
                    _mazeData[x, y] = true;
                }
                else
                {
                    // Closer to center = higher chance to be walkable
                    // Far from center = lower chance
                    float walkableChance = 1.0f - (normalizedDistance * 0.6f);
                    _mazeData[x, y] = Random.value < walkableChance;
                }
            }
        }
        
        // Remove unreachable vertices using flood-fill from start position
        RemoveUnreachableVertices();
        
        // Generate hackable tiles
        GenerateHackableTiles();
    }
    
    void GenerateHackableTiles()
    {
        _hackableTiles.Clear();
        
        // Collect all walkable vertices (except center starting area)
        List<Vector2Int> candidatePositions = new List<Vector2Int>();
        for (int x = 0; x <= mazeWidth; x++)
        {
            for (int y = 0; y <= mazeHeight; y++)
            {
                // Skip center starting area
                if (x >= mazeWidth / 2 - 2 && x <= mazeWidth / 2 + 2 &&
                    y >= mazeHeight / 2 - 2 && y <= mazeHeight / 2 + 2)
                    continue;
                    
                if (_mazeData[x, y])
                {
                    candidatePositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // Randomly select hackable tiles
        for (int i = 0; i < hackableTilesCount && candidatePositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, candidatePositions.Count);
            _hackableTiles.Add(candidatePositions[randomIndex]);
            candidatePositions.RemoveAt(randomIndex);
        }
        
        Debug.Log($"[GridMaze] Generated {_hackableTiles.Count} hackable tiles");
    }
    
    void RemoveUnreachableVertices()
    {
        // Find start position (center)
        Vector2Int start = new Vector2Int(mazeWidth / 2, mazeHeight / 2);
        
        // Track which vertices are reachable
        bool[,] reachable = new bool[mazeWidth + 1, mazeHeight + 1];
        
        // Flood-fill from start to find all reachable vertices
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        reachable[start.x, start.y] = true;
        
        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0)   // Left
        };
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            // Check all 4 neighbors
            foreach (var dir in directions)
            {
                Vector2Int neighbor = current + dir;
                
                // If neighbor is in bounds, walkable, and not yet visited
                if (IsInBounds(neighbor) && _mazeData[neighbor.x, neighbor.y] && !reachable[neighbor.x, neighbor.y])
                {
                    reachable[neighbor.x, neighbor.y] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        // Mark all unreachable vertices as non-walkable
        for (int x = 0; x <= mazeWidth; x++)
        {
            for (int y = 0; y <= mazeHeight; y++)
            {
                if (_mazeData[x, y] && !reachable[x, y])
                {
                    _mazeData[x, y] = false; // Unreachable, make it red
                }
            }
        }
    }

    void CreateVisualMaze() 
    { 
        if (_tilesContainer != null) Destroy(_tilesContainer); 
        _tilesContainer = new GameObject("MazeTiles"); 
        _tilesContainer.transform.parent = transform; 
        
        // Create spheres ONLY at walkable vertices (green only, no red)
        for (int x = 0; x <= mazeWidth; x++) 
        {
            for (int y = 0; y <= mazeHeight; y++) 
            { 
                // Only create sphere if this vertex is walkable
                if (_mazeData[x, y])
                {
                    // Position for this vertex
                    Vector3 pos = new Vector3(
                        (x - mazeWidth / 2f) * cellSize,
                        0,
                        (y - mazeHeight / 2f) * cellSize
                    );
                    
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); 
                    sphere.transform.parent = _tilesContainer.transform; 
                    sphere.transform.position = pos; 
                    sphere.transform.localScale = Vector3.one * (cellSize * 0.3f);
                    sphere.name = "Vertex_" + x + "_" + y;
                    
                    // Color based on tile type
                    Vector2Int gridPos = new Vector2Int(x, y);
                    if (_hackedTiles.Contains(gridPos))
                    {
                        sphere.GetComponent<Renderer>().material.color = hackedTileColor; // Already hacked
                    }
                    else if (_hackableTiles.Contains(gridPos))
                    {
                        sphere.GetComponent<Renderer>().material.color = hackableTileColor; // Hackable
                    }
                    else
                    {
                        sphere.GetComponent<Renderer>().material.color = normalTileColor; // Normal walkable
                    }
                }
            } 
        }
        
        // Create connection lines between adjacent walkable vertices
        if (showConnectionLines)
        {
            CreateConnectionLines();
        }
    }
    
    void CreateConnectionLines()
    {
        if (_linesContainer != null)
            Destroy(_linesContainer);
            
        _linesContainer = new GameObject("ConnectionLines");
        _linesContainer.transform.parent = transform;
        
        int lineCount = 0;
        
        // Check all walkable vertices and connect to adjacent walkable neighbors
        for (int x = 0; x <= mazeWidth; x++)
        {
            for (int y = 0; y <= mazeHeight; y++)
            {
                if (!_mazeData[x, y]) continue; // Skip non-walkable
                
                Vector3 currentPos = new Vector3(
                    (x - mazeWidth / 2f) * cellSize,
                    0,
                    (y - mazeHeight / 2f) * cellSize
                );
                
                // Check right neighbor (to avoid duplicate lines, only check right and up)
                if (x + 1 <= mazeWidth && _mazeData[x + 1, y])
                {
                    Vector3 neighborPos = new Vector3(
                        (x + 1 - mazeWidth / 2f) * cellSize,
                        0,
                        (y - mazeHeight / 2f) * cellSize
                    );
                    CreateLine(currentPos, neighborPos, $"Line_H_{x}_{y}");
                    lineCount++;
                }
                
                // Check up neighbor
                if (y + 1 <= mazeHeight && _mazeData[x, y + 1])
                {
                    Vector3 neighborPos = new Vector3(
                        (x - mazeWidth / 2f) * cellSize,
                        0,
                        (y + 1 - mazeHeight / 2f) * cellSize
                    );
                    CreateLine(currentPos, neighborPos, $"Line_V_{x}_{y}");
                    lineCount++;
                }
            }
        }
        
        Debug.Log($"[GridMaze] Created {lineCount} connection lines");
    }
    
    void CreateLine(Vector3 start, Vector3 end, string lineName)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.parent = _linesContainer.transform;
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        // Set material and color
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = lineColor;
        line.endColor = lineColor;
        
        // Disable shadows and set sort order
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = -1; // Render behind spheres
    }

    public bool IsWalkable(Vector3 worldPosition) { Vector2Int gridPos = WorldToGrid(worldPosition); return IsInBounds(gridPos) && _mazeData[gridPos.x, gridPos.y]; }
    public bool IsWalkable(Vector2Int gridPosition) { return IsInBounds(gridPosition) && _mazeData[gridPosition.x, gridPosition.y]; }
    bool IsInBounds(Vector2Int pos) { return pos.x >= 0 && pos.x <= mazeWidth && pos.y >= 0 && pos.y <= mazeHeight; }

    Vector3 GridToWorld(Vector2Int gridPos) 
    { 
        // Convert grid vertex position to world position
        return new Vector3(
            (gridPos.x - mazeWidth / 2f) * cellSize, 
            0, 
            (gridPos.y - mazeHeight / 2f) * cellSize
        ); 
    }
    
    Vector2Int WorldToGrid(Vector3 worldPos) 
    { 
        // Convert world position to grid vertex position
        int x = Mathf.RoundToInt(worldPos.x / cellSize + mazeWidth / 2f);
        int y = Mathf.RoundToInt(worldPos.z / cellSize + mazeHeight / 2f);
        return new Vector2Int(x, y);
    }

    public Vector3 GetStartPosition() 
    { 
        // Always start at the center of the grid
        Vector2Int centerVertex = new Vector2Int(mazeWidth / 2, mazeHeight / 2);
        return GridToWorld(centerVertex);
    }
    
    public bool IsHackableTile(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        return _hackableTiles.Contains(gridPos) && !_hackedTiles.Contains(gridPos);
    }
    
    public void MarkTileAsHacked(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        if (_hackableTiles.Contains(gridPos))
        {
            _hackedTiles.Add(gridPos);
            Debug.Log($"[GridMaze] Tile {gridPos} hacked! Total hacked: {_hackedTiles.Count}/{_hackableTiles.Count}");
            
            // Update visual
            RefreshTileVisual(gridPos);
        }
    }
    
    void RefreshTileVisual(Vector2Int gridPos)
    {
        // Find and update the sphere color
        string tileName = "Vertex_" + gridPos.x + "_" + gridPos.y;
        Transform tile = _tilesContainer.transform.Find(tileName);
        if (tile != null)
        {
            tile.GetComponent<Renderer>().material.color = hackedTileColor;
        }
    }
    
    public int GetHackedTilesCount()
    {
        return _hackedTiles.Count;
    }
    
    public int GetTotalHackableTiles()
    {
        return _hackableTiles.Count;
    }
    
    public void RegenerateMap()
    {
        // Clear hacked tiles
        _hackedTiles.Clear();
        
        // Destroy old visual elements
        if (_tilesContainer != null)
            Destroy(_tilesContainer);
        if (_linesContainer != null)
            Destroy(_linesContainer);
        
        // Generate new maze and visuals
        GenerateMaze();
        CreateVisualMaze();
        
        // Reset player position to center
        var player = FindFirstObjectByType<GridBallPlayer>();
        if (player != null)
        {
            Vector3 startPos = GridToWorld(new Vector2Int(mazeWidth / 2, mazeHeight / 2));
            player.transform.position = startPos;
            Debug.Log($"[GridMaze] Player reset to center: {startPos}");
        }
        
        Debug.Log("[GridMaze] Map regenerated for second attempt");
    }
}
