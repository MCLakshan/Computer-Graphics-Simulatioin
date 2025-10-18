using UnityEngine;

public class CloudCellularAutomata : MonoBehaviour
{
    [Header("Cloud Settings")]
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 15;
    [SerializeField] private int depth = 30;
    [SerializeField] private float cubeSize = 0.5f;
    
    [Header("Cellular Automata")]
    [SerializeField] private float initialDensity = 0.45f;
    [SerializeField] private int iterations = 4;
    [SerializeField] private int birthThreshold = 13;
    [SerializeField] private int deathThreshold = 12;
    
    [Header("Visuals")]
    [SerializeField] private GameObject cloudPrefab;
    
    private bool[,,] grid;
    private GameObject cloudParent;

    void Start()
    {
        GenerateCloud();
    }

    public void GenerateCloud()
    {
        // Clean up previous cloud
        if (cloudParent != null)
            Destroy(cloudParent);
        
        cloudParent = new GameObject("Cloud");
        cloudParent.transform.parent = transform;
        
        // Initialize grid
        grid = new bool[width, height, depth];
        InitializeGrid();
        
        // Run cellular automata iterations
        for (int i = 0; i < iterations; i++)
        {
            grid = SimulateStep(grid);
        }
        
        // Create visual representation
        CreateCloudMesh();
    }

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    grid[x, y, z] = Random.value < initialDensity;
                }
            }
        }
    }

    bool[,,] SimulateStep(bool[,,] oldGrid)
    {
        bool[,,] newGrid = new bool[width, height, depth];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int neighbors = CountNeighbors(oldGrid, x, y, z);
                    
                    // CA rules for cloud-like formation
                    if (oldGrid[x, y, z])
                    {
                        newGrid[x, y, z] = neighbors >= deathThreshold;
                    }
                    else
                    {
                        newGrid[x, y, z] = neighbors >= birthThreshold;
                    }
                }
            }
        }
        
        return newGrid;
    }

    int CountNeighbors(bool[,,] grid, int x, int y, int z)
    {
        int count = 0;
        
        // Check 26 neighbors (3x3x3 cube minus center)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    int nz = z + dz;
                    
                    // Treat out of bounds as empty
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                    {
                        if (grid[nx, ny, nz]) count++;
                    }
                }
            }
        }
        
        return count;
    }

    void CreateCloudMesh()
    {
        // Create cubes for each active cell
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z])
                    {
                        GameObject cube = Instantiate(cloudPrefab, cloudParent.transform);
                        cube.transform.localPosition = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);
                        cube.transform.localScale = Vector3.one * cubeSize;
                    }
                }
            }
        }
        
        // Center the cloud
        cloudParent.transform.localPosition = new Vector3(-width * cubeSize / 2f, -height * cubeSize / 2f, -depth * cubeSize / 2f);
    }

    [ContextMenu("Regenerate Cloud")]
    public void RegenerateCloud()
    {
        GenerateCloud();
    }
    
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(width * cubeSize, height * cubeSize, depth * cubeSize);
        Gizmos.DrawWireCube(center, size);
    }
}