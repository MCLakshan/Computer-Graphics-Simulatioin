using System.Collections.Generic;
using UnityEngine;

public class InnerTerrainWaterDitection : MonoBehaviour
{
    // Script Description:
    [Header(
        "<b><size=13><color=#66E0FF>Inner Terrain Water Detection Script</color></size></b>\n" +
        "<i><color=#B2F0FF>This script detects water bodies within terrain grid cells and identifies land clusters surrounded by water.</color></i>\n" +
        "<i><color=#B2F0FF> - Analyzes terrain heights to classify grid cells as water (1), land (0), or skip detection (2).</color></i>\n" +
        "<i><color=#B2F0FF> - Uses flood fill algorithm to find connected land clusters completely surrounded by water.</color></i>\n" +
        "<i><color=#B2F0FF> - Adjust 'Water Thresholds' to control water detection sensitivity and 'Skip Detection Thresholds' for exclusion zones.</color></i>"
    )]

    // References
    [Header("<b><color=#89CFF0><size=12>References</size></color></b>")]
    [SerializeField] private TerrainGridFilter terrainGridFilter;
    [SerializeField] private PerlinNoiseTerrainGenerator perlinNoiseTerrainGenerator;
    [SerializeField] private DEBUG_TerrainGridVisualizer debugTerrainGridVisualizer;

    // Water Detection Settings
    [Header("<b><color=#B0F2B6><size=12>Water Detection Settings</size></color></b>")]
    [SerializeField] private int gridSize = 8;
    [Range(0f, 1f)]
    [SerializeField] private float waterLowerThreshold = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float waterUpperThreshold = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float skipDetectionWaterLowerThreshold = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float skipDetectionWaterUpperThreshold = 0.3f;

    // Water Plane Settings
    [Header("<b><color=#F7C59F><size=12>Water Plane Settings</size></color></b>")]
    [Range(0f, 1f)]
    [SerializeField] private float waterHeight = 0.2f;
    [SerializeField] private GameObject waterPlanePrefab;
    [SerializeField] private GameObject waterPlanesParent;
    
    private int[,] waterMap;
    private int[,] waterMapTransposed;
    private List<List<Vector2Int>> waterGridClusters = new List<List<Vector2Int>>();
    
    [ContextMenu("Process Water Detection")]
    public void Process()
    {
        Debug.Log("Process Water Detection");
        // Get the terrain data
        float[,] heights = perlinNoiseTerrainGenerator.GetTerrainHeights();
        float terrainMaxHeight = perlinNoiseTerrainGenerator.GetYRange();
        
        
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
        
        // 2D Array: array[row][column] where row goes DOWN (Y direction) and column goes RIGHT (X direction)
        // But in Unity World : X goes RIGHT, Z goes FORWARD/UP 
        // So we need to flip the array to match Unity's coordinate system
        
        waterMapTransposed = new int[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                waterMapTransposed[z,x] = waterMap[x, z];
            }
        }
        
        // Find clusters of water grids
        FindWaterGridClusters();
        
        // Remove existing water planes if any
        foreach (Transform child in waterPlanesParent.transform)
        {
            Destroy(child.gameObject);
        }
        
        
        // Place water planes for each detected water grid
        for (int i = 0; i < waterGridClusters.Count; i++)
        {
            List<Vector2Int> cluster = waterGridClusters[i];
            foreach (Vector2Int cell in cluster)
            {
                var gridCenterPoint = terrainGridFilter.GetGridCenter(cell.x, cell.y);
                // Create a water plane at the grid center point
                GameObject waterPlane = Instantiate(waterPlanePrefab, new Vector3(gridCenterPoint[0], waterHeight * terrainMaxHeight, gridCenterPoint[1]), Quaternion.identity, waterPlanesParent.transform);
            }
        }
        
        // Hihghlight the water grids in the debug visualizer (Debugging purpose)
        for (int i = 0; i < waterGridClusters.Count; i++)
        {
            List<Vector2Int> cluster = waterGridClusters[i];
            foreach (Vector2Int cell in cluster)
            {
                // Highlight the water grid in the debug visualizer
                debugTerrainGridVisualizer.HighlightCell(cell.x, cell.y);
            }
        }
    }
    
    public int DetectWaterInGrid(int gridX, int gridZ, float[,] heights)
    {
        
        // Get the grid center point
        var gridCenterPoint = terrainGridFilter.GetGridCenter(gridX, gridZ);
        
        // Get the height at the grid center
        float height = heights[(int)gridCenterPoint[0], (int)gridCenterPoint[1]];
        
        // Mark skip detection if the height is within the skip detection thresholds
        if (height < skipDetectionWaterLowerThreshold || height > skipDetectionWaterUpperThreshold)
        {
            return 2; // Skip detection
        }
        
        // Check if the height is within the water thresholds
        if (height >= waterLowerThreshold && height <= waterUpperThreshold)
        {
            return 1;
        }
        
        // No water detected in the grid
        return 0;
    }
    
    // Get the grid clusters that surrounded the water grid
    private void FindWaterGridClusters()
    {
        waterGridClusters.Clear();
        
        // Create a visited array to track processed cells
        bool[,] visited = new bool[gridSize, gridSize];
        
        // Process through the waterMapTransposed
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // If we find a "0" cell that hasn't been visited
                if (waterMapTransposed[x, z] == 0 && !visited[x, z])
                {
                    // Start flood fill to find the cluster
                    List<Vector2Int> cluster = new List<Vector2Int>();
                    FloodFillCluster(x, z, visited, cluster);
                    
                    // Check if this cluster is surrounded by "1" values
                    if (IsClusterSurroundedByWater(cluster))
                    {
                        // Add the cluster to the list of water grid clusters
                        waterGridClusters.Add(cluster);
                    }
                }
            }
        }
        
        // Add outer border cells to each cluster
        AddOuterBorderCells();
    }

    // Flood fill algorithm to find connected "0" cells
    private void FloodFillCluster(int startX, int startZ, bool[,] visited, List<Vector2Int> cluster)
    {
        // Use a stack for iterative flood fill to avoid stack overflow
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startZ));
        
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            int x = current.x;
            int z = current.y;
            
            // Check bounds and if already visited
            if (x < 0 || x >= gridSize || z < 0 || z >= gridSize || visited[x, z])
                continue;
                
            // Check if this is a "0" cell
            if (waterMapTransposed[x, z] != 0)
                continue;
                
            // Mark as visited and add to cluster
            visited[x, z] = true;
            cluster.Add(new Vector2Int(x, z));
            
            // Add neighboring cells to stack (4-directional connectivity)
            stack.Push(new Vector2Int(x + 1, z));     // Right
            stack.Push(new Vector2Int(x - 1, z));     // Left
            stack.Push(new Vector2Int(x, z + 1));     // Up
            stack.Push(new Vector2Int(x, z - 1));     // Down
        }
    }

    // Check if a cluster is completely surrounded by "1" values
    private bool IsClusterSurroundedByWater(List<Vector2Int> cluster)
    {
        foreach (Vector2Int cell in cluster)
        {
            int x = cell.x;
            int z = cell.y;
            
            // Check all 8 neighboring cells (including diagonals)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue; // Skip the cell itself
                    
                    int neighborX = x + dx;
                    int neighborZ = z + dz;
                    
                    // If neighbor is outside grid bounds, consider it as not surrounded
                    if (neighborX < 0 || neighborX >= gridSize || neighborZ < 0 || neighborZ >= gridSize)
                    {
                        return false;
                    }
                    
                    // If neighbor is not part of this cluster and is not "1", then not surrounded
                    if (!cluster.Contains(new Vector2Int(neighborX, neighborZ)) && waterMapTransposed[neighborX, neighborZ] != 1)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    // Add outer border cells to each cluster
    private void AddOuterBorderCells()
    {
        // Process each cluster to add border cells
        for (int clusterIndex = 0; clusterIndex < waterGridClusters.Count; clusterIndex++)
        {
            List<Vector2Int> cluster = waterGridClusters[clusterIndex];
            List<Vector2Int> borderCells = new List<Vector2Int>();
        
            // Find all border cells for this cluster
            foreach (Vector2Int cell in cluster)
            {
                int x = cell.x;
                int z = cell.y;
            
                // Check all 8 neighboring cells (including diagonals)
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue; // Skip the cell itself
                    
                        int neighborX = x + dx;
                        int neighborZ = z + dz;
                    
                        // Check if neighbor is within grid bounds
                        if (neighborX >= 0 && neighborX < gridSize && neighborZ >= 0 && neighborZ < gridSize)
                        {
                            Vector2Int neighborCell = new Vector2Int(neighborX, neighborZ);
                        
                            // If neighbor is not part of the cluster and not already in border list
                            if (!cluster.Contains(neighborCell) && !borderCells.Contains(neighborCell))
                            {
                                borderCells.Add(neighborCell);
                            }
                        }
                    }
                }
            }
        
            // Add all border cells to the cluster
            cluster.AddRange(borderCells);
        
            Debug.Log($"Cluster {clusterIndex}: Added {borderCells.Count} border cells to {cluster.Count - borderCells.Count} original cells");
        }
    }
    
}
