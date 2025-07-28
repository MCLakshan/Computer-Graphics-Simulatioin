using UnityEngine;

public class InnerTerrainWaterDitection : MonoBehaviour
{
    // References
    [Header("References")]
    [SerializeField] private TerrainGridFilter terrainGridFilter;
    [SerializeField] private PerlinNoiseTerrainGenerator perlinNoiseTerrainGenerator;
    [SerializeField] private DEBUG_TerrainGridVisualizer debugTerrainGridVisualizer;
    [SerializeField] private int gridSize = 8;
    [SerializeField] private float waterLowerThreshold = 0.1f;
    [SerializeField] private float waterUpperThreshold = 0.3f;

    private int[,] waterMap;
    
    [ContextMenu("Process Water Detection")]
    public void Process()
    {
        Debug.Log("Process Water Detection");
        // Get the terrain data
        float[,] heights = perlinNoiseTerrainGenerator.GetTerrainHeights();
        
        
        // Initialize the water map all to zero
        waterMap = new int[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                waterMap[x, z] = 0;
            }
        }

        
        // Loop through each grid cell
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Check if water is detected in the current grid cell
                //waterMap[x, z] = DetectWaterInGrid(x, z, heights);
                
                if(x == 0 && z == 0)
                {
                    // Debug log for the first grid cell
                    Debug.Log($"Processing grid cell ({x}, {z})");
                    waterMap[x, z] = DetectWaterInGrid(x, z, heights, true);
                }
                else
                {
                    waterMap[x, z] = DetectWaterInGrid(x, z, heights, false);
                }
            }
        }
        
        // Highlight the water grid in the editor
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (waterMap[z, x] == 1)
                {
                    if (x == 0 && z == 0)
                    {
                        Debug.Log("waterMap[" + x + ", " + z + "] = " + waterMap[x, z]);
                    }
                    
                    // Highlight the grid cell if water is detected
                    debugTerrainGridVisualizer.HighlightCell(x, z);
                }
            }
        }
    }
    
    public int DetectWaterInGrid(int gridX, int gridZ, float[,] heights, bool print)
    {
        
        // Get the grid center point
        var gridCenterPoint = terrainGridFilter.GetGridCenter(gridX, gridZ);
        
        // Get the height at the grid center
        float height = heights[(int)gridCenterPoint[0], (int)gridCenterPoint[1]];

        if (print)
        {
            Debug.Log("Grid Center Point: " + gridCenterPoint);
            Debug.Log("Height at Grid Center: " + height);
        }
        
        // Check if the height is within the water thresholds
        if (height >= waterLowerThreshold && height <= waterUpperThreshold)
        {
            if (print)
            {
                Debug.Log($"Water detected in grid ({gridX}, {gridZ}) with height: {height}");
            }
            return 1;
        }
        
        if (print)
        {
            Debug.Log($"No water detected in grid ({gridX}, {gridZ}) with height: {height}");
        }
        // No water detected in the grid
        return 0;
    }
}
