using UnityEngine;
using System.Collections.Generic;

public class DEBUG_TerrainGridVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 8;
    [SerializeField] private Color gridColor = Color.yellow;
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showLabels = true;
    [SerializeField] private bool useDots = false;
    [SerializeField] private float dotSize = 0.5f;
    
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private bool showHighlightedCells = true;
    [SerializeField] private float highlightAlpha = 0.3f;
    
    [Header("Reference")]
    [SerializeField] private PerlinNoiseTerrainGenerator terrainGenerator;
    
    // FIXED: Using Vector3Int instead of Vector2Int to properly handle x,z coordinates
    private HashSet<Vector3Int> highlightedCells = new HashSet<Vector3Int>();
    
    private void OnValidate()
    {
        // Auto-find terrain generator if not assigned
        if (terrainGenerator == null)
            terrainGenerator = GetComponent<PerlinNoiseTerrainGenerator>();
    }

    /// <summary>
    /// Highlights a specific grid cell at the given coordinates
    /// </summary>
    /// <param name="x">X coordinate of the grid cell</param>
    /// <param name="z">Z coordinate of the grid cell</param>
    public void HighlightCell(int x, int z)
    {
        if (IsValidGridCoordinate(x, z))
        {
            // FIXED: Using Vector3Int with proper x,y=0,z coordinates
            highlightedCells.Add(new Vector3Int(x, 0, z));
        }
        else
        {
            Debug.LogWarning($"Invalid grid coordinates ({x}, {z}). Valid range is 0 to {gridSize - 1}");
        }
    }
    
    /// <summary>
    /// Removes highlight from a specific grid cell
    /// </summary>
    /// <param name="x">X coordinate of the grid cell</param>
    /// <param name="z">Z coordinate of the grid cell</param>
    public void UnhighlightCell(int x, int z)
    {
        // FIXED: Using Vector3Int with proper x,y=0,z coordinates
        highlightedCells.Remove(new Vector3Int(x, 0, z));
    }
    
    /// <summary>
    /// Clears all highlighted cells
    /// </summary>
    public void ClearHighlights()
    {
        highlightedCells.Clear();
    }
    
    /// <summary>
    /// Highlights multiple cells at once
    /// </summary>
    /// <param name="coordinates">Array of Vector2Int coordinates to highlight</param>
    public void HighlightCells(Vector2Int[] coordinates)
    {
        foreach (var coord in coordinates)
        {
            // FIXED: Using coord.x and coord.y as z coordinate (Vector2Int.y represents z in grid terms)
            HighlightCell(coord.x, coord.y);
        }
    }
    
    /// <summary>
    /// Checks if the given coordinates are within the valid grid range
    /// </summary>
    private bool IsValidGridCoordinate(int x, int z)
    {
        return x >= 0 && x < gridSize && z >= 0 && z < gridSize;
    }
    
    /// <summary>
    /// Returns true if the specified cell is currently highlighted
    /// </summary>
    public bool IsCellHighlighted(int x, int z)
    {
        // FIXED: Using Vector3Int with proper x,y=0,z coordinates
        return highlightedCells.Contains(new Vector3Int(x, 0, z));
    }

    private void OnDrawGizmos()
    {
        // Exit if grid is disabled or no terrain generator
        if (!showGrid || terrainGenerator == null) return;
        
        // Get terrain size
        float terrainSize = terrainGenerator.GetXRange();
        float cellSize = terrainSize / gridSize;
        
        // Get terrain position
        Vector3 terrainPos = transform.position;
        float yPos = terrainPos.y + 5f; // Draw lines 5 units above terrain
        
        // Draw highlighted cells first (so they appear behind the grid lines)
        if (showHighlightedCells)
        {
            DrawHighlightedCells(terrainPos, cellSize, yPos);
        }
        
        // Set gizmo color for grid lines
        Gizmos.color = gridColor;
        
        // Draw vertical lines
        for (int x = 0; x <= gridSize; x++)
        {
            float xPos = terrainPos.x + (x * cellSize);
            Vector3 start = new Vector3(xPos, yPos, terrainPos.z);
            Vector3 end = new Vector3(xPos, yPos, terrainPos.z + terrainSize);
            Gizmos.DrawLine(start, end);
        }
        
        // Draw horizontal lines
        for (int z = 0; z <= gridSize; z++)
        {
            float zPos = terrainPos.z + (z * cellSize);
            Vector3 start = new Vector3(terrainPos.x, yPos, zPos);
            Vector3 end = new Vector3(terrainPos.x + terrainSize, yPos, zPos);
            Gizmos.DrawLine(start, end);
        }
        
        // Draw region labels or dots
        if (showLabels)
        {
            if (useDots)
            {
                DrawDots(terrainPos, cellSize, yPos);
            }
            else
            {
                DrawLabels(terrainPos, cellSize, yPos);
            }
        }
    }
    
    private void DrawHighlightedCells(Vector3 terrainPos, float cellSize, float yPos)
    {
        // Set highlight color with alpha
        Color highlightColorWithAlpha = highlightColor;
        highlightColorWithAlpha.a = highlightAlpha;
        Gizmos.color = highlightColorWithAlpha;
        
        foreach (var cell in highlightedCells)
        {
            // FIXED: Now using cell.x and cell.z properly instead of cell.y
            Vector3 center = new Vector3(
                terrainPos.x + (cell.x * cellSize) + (cellSize * 0.5f),
                yPos,
                terrainPos.z + (cell.z * cellSize) + (cellSize * 0.5f)  // Now uses cell.z instead of cell.y
            );
            
            // Draw a simple cube instead of complex mesh
            Gizmos.DrawCube(center, new Vector3(cellSize, 0.1f, cellSize));
        }
    }
    
    private void DrawDots(Vector3 terrainPos, float cellSize, float yPos)
    {
        #if UNITY_EDITOR
        // Store original zTest setting
        UnityEngine.Rendering.CompareFunction originalZTest = UnityEditor.Handles.zTest;
        
        // Set zTest to Always so dots are always visible
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        
        // Draw dots for each grid cell center
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // FIXED: Using Vector3Int with proper x,y=0,z coordinates
                Vector3Int cellCoord = new Vector3Int(x, 0, z);
                UnityEditor.Handles.color = highlightedCells.Contains(cellCoord) ? highlightColor : gridColor;
                
                // Calculate center of grid cell
                float centerX = terrainPos.x + (x * cellSize) + (cellSize * 0.5f);
                float centerZ = terrainPos.z + (z * cellSize) + (cellSize * 0.5f);
                Vector3 dotPos = new Vector3(centerX, yPos + 1f, centerZ);
                
                // Draw a sphere using Handles for better control
                UnityEditor.Handles.SphereHandleCap(0, dotPos, Quaternion.identity, dotSize * 2f, EventType.Repaint);
            }
        }
        
        // Restore original zTest setting
        UnityEditor.Handles.zTest = originalZTest;
        #endif
    }
    
    private void DrawLabels(Vector3 terrainPos, float cellSize, float yPos)
    {
        #if UNITY_EDITOR
        // Create a custom GUI style for better text appearance
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        
        // Draw label for each grid cell
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // FIXED: Using Vector3Int with proper x,y=0,z coordinates
                Vector3Int cellCoord = new Vector3Int(x, 0, z);
                labelStyle.normal.textColor = highlightedCells.Contains(cellCoord) ? highlightColor : gridColor;
                
                // Calculate center of grid cell
                float centerX = terrainPos.x + (x * cellSize) + (cellSize * 0.5f);
                float centerZ = terrainPos.z + (z * cellSize) + (cellSize * 0.5f);
                Vector3 labelPos = new Vector3(centerX, yPos + 2f, centerZ);
                
                // Draw the label with custom style
                UnityEditor.Handles.Label(labelPos, $"R[{x},{z}]", labelStyle);
            }
        }
        #endif
    }
    
    // Example usage methods for testing
    [ContextMenu("Test Highlight Cell (2,3)")]
    private void TestHighlightCell()
    {
        HighlightCell(2, 3);
    }
    
    [ContextMenu("Test Highlight Multiple Cells")]
    private void TestHighlightMultipleCells()
    {
        Vector2Int[] testCells = { 
            new Vector2Int(0, 0), 
            new Vector2Int(1, 1), 
            new Vector2Int(3, 4) 
        };
        HighlightCells(testCells);
    }
    
    [ContextMenu("Clear All Highlights")]
    private void TestClearHighlights()
    {
        ClearHighlights();
    }
}