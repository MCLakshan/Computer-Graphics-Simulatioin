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
                    waterMap[x, z] = DetectWaterInGrid(x, z, heights);
                }
                else
                {
                    waterMap[x, z] = DetectWaterInGrid(x, z, heights);
                }
            }
        }
        
        // Highlight the water grid in the editor
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (waterMap[x, z] == 1)
                {
                    // 2D Array: array[row][column] where row goes DOWN (Y direction) and column goes RIGHT (X direction)
                    // But in Unity World : X goes RIGHT, Z goes FORWARD/UP 
                    debugTerrainGridVisualizer.HighlightCell(z, x); 
                }
            }
        }
    }
    
    public int DetectWaterInGrid(int gridX, int gridZ, float[,] heights)
    {
        
        // Get the grid center point
        var gridCenterPoint = terrainGridFilter.GetGridCenter(gridX, gridZ);
        
        // Get the height at the grid center
        float height = heights[(int)gridCenterPoint[0], (int)gridCenterPoint[1]];
        
        // Check if the height is within the water thresholds
        if (height >= waterLowerThreshold && height <= waterUpperThreshold)
        {
            return 1;
        }
        
        // No water detected in the grid
        return 0;
    }
}
