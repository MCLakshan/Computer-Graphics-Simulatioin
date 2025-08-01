using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SIMPLE TERRAIN OBJECT SPAWNER WITH BLUE NOISE
// Gets terrain heights and spawns objects based on blue noise distribution
public class TerrainObjectSpawner : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;
    
    [Tooltip("Prefab to spawn (tree, rock, etc.)")]
    public GameObject prefabToSpawn;

    [Header("Blue Noise Settings")]
    [Tooltip("Blue noise texture (PNG) for natural distribution - maps 1:1 with terrain size")]
    public Texture2D blueNoiseTexture;

    [Range(0f, 1f)]
    [Tooltip("Only spawn where noise > this value. Higher = fewer objects")]
    public float noiseThreshold = 0.5f;

    [Header("Spawn Settings")]
    [Tooltip("Exact number of objects to spawn")]
    public int spawnCount = 1000;

    [Tooltip("Minimum distance between objects")]
    public float minDistanceBetweenObjects = 5f;

    [Header("Height Constraints (0-1 percentages)")]
    [Range(0f, 1f)]
    [Tooltip("Don't spawn below this height percentage (0 = lowest, 1 = highest)")]
    public float minHeightPercent = 0.2f;

    [Range(0f, 1f)]
    [Tooltip("Don't spawn above this height percentage (0 = lowest, 1 = highest)")]
    public float maxHeightPercent = 0.8f;

    [Header("Randomization")]
    [Tooltip("Random rotation on Y axis")]
    public bool randomRotation = true;

    [Header("Performance")]
    [Tooltip("Process this many positions per frame to avoid lag")]
    public int positionsPerFrame = 100;

    [Header("User Interface")]
    [SerializeField] private string spawningObjectName;
    [SerializeField] private TMP_Text poggressBarText;
    [SerializeField] private Slider spawnCountSlider;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool visualizeSpawnArea = false;
    
    // Private variables
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private bool isSpawning = false;
    
    [ContextMenu("Spawn Objects")]
    public void SpawnObjects()
    {
        if (isSpawning) return;
        if (terrain == null || prefabToSpawn == null) return;
        
        // UI slider for spawn count
        spawnCountSlider.value = 0f;
        
        StartCoroutine(SpawnObjectsCoroutine());
    }
    
    System.Collections.IEnumerator SpawnObjectsCoroutine()
    {
        isSpawning = true;
        
        // Clear existing objects
        ClearObjects();
        
        // Get terrain data
        float[,] heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;
        int heightmapRes = terrain.terrainData.heightmapResolution;
        
        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = spawnCount * 10; // Prevent infinite loop
        
        if (showDebugInfo)
            Debug.Log($"Starting to spawn {spawnCount} objects");
        
        // Keep spawning until we reach exact count
        while (spawnedCount < spawnCount && attempts < maxAttempts)
        {
            int processedThisFrame = 0;
            
            while (processedThisFrame < positionsPerFrame && spawnedCount < spawnCount && attempts < maxAttempts)
            {
                // Generate random position
                int x = Random.Range(0, heightmapRes);
                int z = Random.Range(0, heightmapRes);
                
                // Get height percentage
                float heightPercent = heightMap[z, x];
                
                // Check height constraints
                if (heightPercent >= minHeightPercent && heightPercent <= maxHeightPercent)
                {
                    // Convert to world position
                    Vector3 worldPos = new Vector3(
                        terrainPos.x + ((float)x / (heightmapRes - 1)) * terrainSize.x,
                        terrainPos.y + heightPercent * terrainSize.y,
                        terrainPos.z + ((float)z / (heightmapRes - 1)) * terrainSize.z
                    );
                    
                    // Check blue noise and distance constraints
                    if (ShouldSpawnAtPosition(worldPos) && IsValidPosition(worldPos))
                    {
                        SpawnObjectAt(worldPos);
                        spawnedCount++;
                    }
                }
                
                // Update UI
                poggressBarText.text = "Generating "+ spawningObjectName + " : Attempts - " + attempts + " / Spawned - " + spawnedCount + " / Target - " + spawnCount;
                spawnCountSlider.value = (float)spawnedCount / spawnCount;
                
                attempts++;
                processedThisFrame++;
            }
            
            yield return null; // Wait one frame
        }
        
        if (showDebugInfo)
        {
            if (spawnedCount == spawnCount)
                Debug.Log($"Successfully spawned exactly {spawnedCount} objects in {attempts} attempts");
            else
                Debug.LogWarning($"Could only spawn {spawnedCount} out of {spawnCount} objects after {attempts} attempts. Try adjusting constraints or increasing terrain size.");
        }
            
        isSpawning = false;
    }
    
    bool ShouldSpawnAtPosition(Vector3 worldPos)
    {
        if (blueNoiseTexture == null)
        {
            return true; // No noise texture, always spawn (subject to other constraints)
        }
        
        // Get terrain bounds
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        
        // Convert world position to normalized UV coordinates (0-1) within terrain bounds
        Vector2 noiseUV = new Vector2(
            (worldPos.x - terrainPos.x) / terrainSize.x,
            (worldPos.z - terrainPos.z) / terrainSize.z
        );
        
        // Clamp UV to 0-1 range to stay within texture bounds
        noiseUV.x = Mathf.Clamp01(noiseUV.x);
        noiseUV.y = Mathf.Clamp01(noiseUV.y);
        
        // Sample noise texture - use grayscale value as spawn probability
        Color noiseValue = blueNoiseTexture.GetPixelBilinear(noiseUV.x, noiseUV.y);
        float noiseIntensity = noiseValue.grayscale;
        
        // Only spawn if noise value is above threshold
        return noiseIntensity > noiseThreshold;
    }
    
    bool IsValidPosition(Vector3 position)
    {
        // Check distance to other spawned objects
        foreach (Vector3 existingPos in spawnedPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenObjects)
                return false;
        }
        return true;
    }
    
    void SpawnObjectAt(Vector3 position)
    {
        Quaternion rotation = randomRotation ? Quaternion.Euler(0, Random.Range(0, 360), 0) : Quaternion.identity;
        
        GameObject spawnedObject = Instantiate(prefabToSpawn, position, rotation);
        spawnedObject.transform.SetParent(this.transform);
        
        spawnedPositions.Add(position);
        spawnedObjects.Add(spawnedObject);
    }
    
    [ContextMenu("Clear Objects")]
    public void ClearObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        spawnedObjects.Clear();
        spawnedPositions.Clear();
    }
    
    void OnDrawGizmos()
    {
        if (!visualizeSpawnArea || terrain == null) return;
        
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 center = terrainPos + terrainSize * 0.5f;
        center.y = terrainPos.y;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, terrainSize);
        
        Gizmos.color = Color.red;
        foreach (Vector3 pos in spawnedPositions)
        {
            Gizmos.DrawWireSphere(pos, minDistanceBetweenObjects * 0.5f);
        }
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying || !showDebugInfo) return;
        
        GUI.Label(new Rect(10, 10, 300, 20), $"Spawned Objects: {spawnedObjects.Count}");
        
        if (GUI.Button(new Rect(10, 40, 120, 30), "Spawn Objects"))
            SpawnObjects();
            
        if (GUI.Button(new Rect(140, 40, 120, 30), "Clear Objects"))
            ClearObjects();
    }
}

/*
SIMPLE USAGE:
1. Assign Terrain to "Terrain" field
2. Assign prefab to "Prefab To Spawn" 
3. Assign blue noise texture (optional)
4. Set height constraints as percentages (0-1)
5. Click "Spawn Objects"

CORE CONCEPT:
• Gets terrain heights from terrain.terrainData.GetHeights()
• Gets terrain size from terrain.terrainData.size
• Spawns objects on terrain surface using blue noise distribution
• Height constraints work as percentages of terrain height
*/