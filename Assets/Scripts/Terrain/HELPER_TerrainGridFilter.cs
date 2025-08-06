using UnityEngine;

[System.Serializable]
public struct GridBounds
{
    public int startX;
    public int startZ;
    public int endX;
    public int endZ;
    
    public GridBounds(int startX, int startZ, int endX, int endZ)
    {
        this.startX = startX;
        this.startZ = startZ;
        this.endX = endX;
        this.endZ = endZ;
    }
    
    public override string ToString()
    {
        return $"GridBounds(Start: [{startX},{startZ}], End: [{endX},{endZ}])";
    }
}

public class HELPER_TerrainGridFilter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PerlinNoiseTerrainGenerator terrainGenerator;
    
    //[Header("Grid Settings")]
    //[SerializeField] private int gridSize = 8;
    
    private void OnValidate()
    {
        // Auto-find terrain generator if not assigned
        if (terrainGenerator == null)
            terrainGenerator = GetComponent<PerlinNoiseTerrainGenerator>();
    }
    
    /// <summary>
    /// Get grid bounds for a specific region by name (e.g., "R[2,3]")
    /// </summary>
    /// <param name="gridName">Grid name in format "R[x,z]"</param>
    /// <returns>GridBounds with start and end coordinates</returns>
    public GridBounds GetGridBounds(string gridName, int gridSize)
    {
        Vector2Int coords = ParseGridName(gridName);
        
        if (coords.x == -1 || coords.y == -1)
        {
            Debug.LogError($"Invalid grid name format: {gridName}. Use format 'R[x,z]'");
            return new GridBounds(-1, -1, -1, -1);
        }
        
        return GetGridBounds(coords.x, coords.y, gridSize);
    }
    
    /// <summary>
    /// Get grid bounds for a specific region by coordinates
    /// </summary>
    /// <param name="gridX">X coordinate of the grid</param>
    /// <param name="gridZ">Z coordinate of the grid</param>
    /// <returns>GridBounds with start and end coordinates</returns>
    public GridBounds GetGridBounds(int gridX, int gridZ, int gridSize)
    {
        if (terrainGenerator == null)
        {
            Debug.LogError("TerrainGenerator reference is missing!");
            return new GridBounds(-1, -1, -1, -1);
        }
        
        // Get terrain and grid info
        int terrainSize = (int)terrainGenerator.GetXRange();
        
        // Validate coordinates
        if (gridX < 0 || gridX >= gridSize || gridZ < 0 || gridZ >= gridSize)
        {
            Debug.LogError($"Grid coordinates [{gridX},{gridZ}] are out of range. Grid size is {gridSize}x{gridSize}");
            return new GridBounds(-1, -1, -1, -1);
        }
        
        // Calculate cell size
        float cellSize = (float)terrainSize / gridSize;
        
        // Calculate start and end coordinates
        int startX = Mathf.FloorToInt(gridX * cellSize);
        int startZ = Mathf.FloorToInt(gridZ * cellSize);
        int endX = Mathf.FloorToInt((gridX + 1) * cellSize) - 1; // -1 to make it inclusive
        int endZ = Mathf.FloorToInt((gridZ + 1) * cellSize) - 1; // -1 to make it inclusive
        
        // Clamp to terrain bounds
        endX = Mathf.Min(endX, terrainSize - 1);
        endZ = Mathf.Min(endZ, terrainSize - 1);
        
        return new GridBounds(startX, startZ, endX, endZ);
    }
    
    /// <summary>
    /// Get the center point of a specific grid cell
    /// </summary>
    /// <param name="gridX">X coordinate of the grid</param>
    /// <param name="gridZ">Z coordinate of the grid</param>
    /// <returns>Vector2 with center coordinates</returns>
    public Vector2 GetGridCenter(int gridX, int gridZ, int gridSize)
    {
        GridBounds bounds = GetGridBounds(gridX, gridZ, gridSize);
    
        if (bounds.startX == -1) // Invalid bounds
            return Vector2.zero;
    
        float centerX = (bounds.startX + bounds.endX + 1) * 0.5f; // +1 to include the end point
        float centerZ = (bounds.startZ + bounds.endZ + 1) * 0.5f;
    
        return new Vector2(centerX, centerZ);
    }

    /// <summary>
    /// Get the center point of a specific grid cell by name
    /// </summary>
    /// <param name="gridName">Grid name in format "R[x,z]"</param>
    /// <returns>Vector2 with center coordinates</returns>
    public Vector2 GetGridCenter(string gridName, int gridSize)
    {
        Vector2Int coords = ParseGridName(gridName);
    
        if (coords.x == -1 || coords.y == -1)
            return Vector2.zero;
    
        return GetGridCenter(coords.x, coords.y, gridSize);
    }
    
    /// <summary>
    /// Parse grid name string to extract coordinates
    /// </summary>
    /// <param name="gridName">Grid name in format "R[x,z]"</param>
    /// <returns>Vector2Int with coordinates, or (-1,-1) if invalid</returns>
    private Vector2Int ParseGridName(string gridName)
    {
        if (string.IsNullOrEmpty(gridName))
            return new Vector2Int(-1, -1);
        
        // Remove spaces and convert to uppercase
        gridName = gridName.Replace(" ", "").ToUpper();
        
        // Check if it starts with "R[" and ends with "]"
        if (!gridName.StartsWith("R[") || !gridName.EndsWith("]"))
            return new Vector2Int(-1, -1);
        
        // Extract the coordinates part (remove "R[" and "]")
        string coordsString = gridName.Substring(2, gridName.Length - 3);
        
        // Split by comma
        string[] coords = coordsString.Split(',');
        if (coords.Length != 2)
            return new Vector2Int(-1, -1);
        
        // Parse coordinates
        if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int z))
        {
            return new Vector2Int(x, z);
        }
        
        return new Vector2Int(-1, -1);
    }
}
