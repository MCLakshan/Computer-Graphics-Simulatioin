using UnityEngine;

public class SimpleTerrainGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 8;
    [SerializeField] private Color gridColor = Color.yellow;
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showLabels = true;
    
    [Header("Reference")]
    [SerializeField] private PerlinNoiseTerrainGenerator terrainGenerator;
    
    private void OnValidate()
    {
        // Auto-find terrain generator if not assigned
        if (terrainGenerator == null)
            terrainGenerator = GetComponent<PerlinNoiseTerrainGenerator>();
    }

    private void OnDrawGizmos()
    {
        // Exit if grid is disabled or no terrain generator
        if (!showGrid || terrainGenerator == null) return;
        
        // Get terrain size
        float terrainSize = terrainGenerator.GetXRange();
        float cellSize = terrainSize / gridSize;
        
        // Set gizmo color
        Gizmos.color = gridColor;
        
        // Get terrain position
        Vector3 terrainPos = transform.position;
        float yPos = terrainPos.y + 5f; // Draw lines 5 units above terrain
        
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
        
        // Draw region labels
        if (showLabels)
        {
            DrawLabels(terrainPos, cellSize, yPos);
        }
    }
    
    private void DrawLabels(Vector3 terrainPos, float cellSize, float yPos)
    {
        #if UNITY_EDITOR
        // Create a custom GUI style for better text appearance
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = gridColor;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        
        // Draw label for each grid cell
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
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
}