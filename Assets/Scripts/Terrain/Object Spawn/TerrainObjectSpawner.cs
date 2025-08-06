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

    [Header("Blue Noise Settings")]
    [Tooltip("Blue noise texture (PNG) for natural distribution - maps 1:1 with terrain size")]
    public Texture2D blueNoiseTexture;

    [Range(0f, 1f)]
    [Tooltip("Only spawn where noise > this value. Higher = fewer objects")]
    public float noiseThreshold = 0.5f;
    
    [Header("Spawn Objects")]
    [SerializeField] ObjectToSpawn[] listOfObjectsToSpawn;

    [Header("Randomization")]
    [Tooltip("Random rotation on Y axis")]
    public bool randomRotation = true;

    [Header("Performance")]
    [Tooltip("Process this many positions per frame to avoid lag")]
    public int positionsPerFrame = 100;
    public int maxSpawnAttemptsMultiplier = 100; // Max attempts per object type to prevent infinite loops

    [Header("User Interface")]
    [SerializeField] private string spawningObjectName;
    [SerializeField] private TMP_Text poggressBarText;
    [SerializeField] private Slider spawnCountSlider;

    [Header("Debug")]
    public bool visualizeSpawnArea = false;
    
    // Private variables
    private List<(Vector3, SpawnType)> spawnedPositions = new List<(Vector3, SpawnType)>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private bool isSpawning = false;
    
    [ContextMenu("Spawn Objects")]
    public void SpawnObjects()
    {
        if (isSpawning) return;
        if (terrain == null) return;
        
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
        
        // Track Spawned count
        int currentSpawnedCount = 0;
        int currentAttemptCount = 0;
        int totalSpawnedCount = 0;
        foreach (ObjectToSpawn objectToSpawn in listOfObjectsToSpawn)
        {
            totalSpawnedCount += objectToSpawn.SpawnCount;
        }
        
        // for each object type Do spawn
        foreach (var objectToSpawn in listOfObjectsToSpawn)
        {
            // Skip if not allowed to spawn
            if (!objectToSpawn.canSpawn)
            {
                continue;
            }
            
            int spawnedCount = 0;
            int attempts = 0;
            int spawnCount = objectToSpawn.SpawnCount;
            GameObject prefabToSpawn = objectToSpawn.Prefab;
            float minHeightPercent = objectToSpawn.MinHeightPercent;
            float maxHeightPercent = objectToSpawn.MaxHeightPercent;
            float minDistanceBetweenObjects = objectToSpawn.MindistanceBetweenType;
            SpawnType spawnType = objectToSpawn.Type;
            int maxAttempts = spawnCount * maxSpawnAttemptsMultiplier; // Prevent infinite loop
            
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
                        if (ShouldSpawnAtPosition(worldPos) && IsValidPosition(worldPos, minDistanceBetweenObjects, spawnType))
                        {
                            SpawnObjectAt(worldPos, spawnType, prefabToSpawn);
                            spawnedCount++;
                            currentSpawnedCount++;
                        }
                    }
                
                    // Update UI
                    poggressBarText.text = "Generating "+ spawningObjectName + " : Attempts(" + maxAttempts + ") - " + currentAttemptCount + " / Spawned - " + currentSpawnedCount + " / Target - " + totalSpawnedCount;
                    spawnCountSlider.value = (float)currentSpawnedCount / totalSpawnedCount;
                
                    attempts++;
                    currentAttemptCount++;
                    processedThisFrame++;
                }
            
                yield return null; // Wait one frame
            }
            Debug.Log($"Spawned {spawnedCount} of {spawnCount} form  {attempts}/{maxAttempts} attempts for {objectToSpawn.Name}");
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
    
    bool IsValidPosition(Vector3 position, float minDistanceBetweenObjects, SpawnType spawnType)
    {
        // Check distance to other spawned objects
        foreach (var (pos, type) in spawnedPositions)
        {
            if (Vector3.Distance(position, pos) < minDistanceBetweenObjects && type == spawnType)
                return false;
        }
        return true;
    }
    
    void SpawnObjectAt(Vector3 position, SpawnType spawnType, GameObject prefabToSpawn)
    {
        Quaternion rotation = randomRotation ? Quaternion.Euler(0, Random.Range(0, 360), 0) : Quaternion.identity;
        
        GameObject spawnedObject = Instantiate(prefabToSpawn, position, rotation);
        spawnedObject.transform.SetParent(this.transform);
        
        spawnedPositions.Add((position, spawnType));
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
        foreach (var (pos, type ) in spawnedPositions)
        {
            Gizmos.DrawWireSphere(pos, 1f);
        }
    }
}

[System.Serializable]
class ObjectToSpawn
{
    // All the properties that need to spawn an object
    public string Name;
    public SpawnType Type;
    public GameObject Prefab;
    public int SpawnCount;
    public float MindistanceBetweenType;
    [Range(0f, 1f)]
    public float MinHeightPercent;
    [Range(0f, 1f)]
    public float MaxHeightPercent;
    public bool canSpawn = true;
}

[System.Serializable]
public enum SpawnType
{
    Grass_Tree,
    Forest_Tree,
    Thundra_Tree,
    Rock,
    Bush,
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