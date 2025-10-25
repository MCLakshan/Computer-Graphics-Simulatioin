using UnityEngine;
using System.Collections.Generic;

public class FogChunkSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Mng_GlobalReferences globalRefs;
    [SerializeField] private GameObject fogPrefab; // Your fog unit prefab
    
    [Header("GPU Instancing Master")]
    [SerializeField] private ProceduralObjectSpawnerGPUMaster master;
    
    [Header("Chunk Settings")]
    [SerializeField] private float chunkSize = 32f; // Size of one fog unit
    [SerializeField] private int chunksAroundPlayer = 5; // How many chunks in each direction
    
    [Header("Optional - Distance Based")]
    [SerializeField] private bool useDistanceInstead = false;
    [SerializeField] private float spawnDistance = 100f; // Only used if useDistanceInstead = true
    
    [Header("Height Settings")]
    [SerializeField] private float heightOffset = 0f; // Offset above terrain height
    [SerializeField] private float minHeightLimit = -Mathf.Infinity; // Minimum height to spawn fog
    [SerializeField] private float maxHeightLimit = Mathf.Infinity; // Maximum height to spawn fog
    
    [Header("Performance")]
    [SerializeField] private float updateInterval = 1f; // How often to check (in seconds)
    
    private Transform player;
    private Terrain terrain;
    private float maxTerrainHeight; // Max height of terrain for bounds checking
    
    // Tracking
    private Dictionary<Vector2Int, GameObject> spawnedFogChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;
    private float updateTimer = 0f;
    
    void OnEnable()
    {
        master.onPlayerMovedToNewChunk += UpdateFogChunks;
    }
    
    void Start()
    {
        if (globalRefs == null)
        {
            Debug.LogError("Global References not assigned!");
            return;
        }

        player = globalRefs.GetPlayer();
        terrain = globalRefs.GetTerrain();
        
        if (fogPrefab == null)
        {
            Debug.LogError("Fog prefab is missing!");
            return;
        }
        
        // Initial spawn
        lastPlayerChunk = GetChunkCoord(player.position);
        UpdateFogChunks();
    }
    
    void UpdateFogChunks()
    {
        Vector2Int playerChunk = GetChunkCoord(player.position);
        HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();
        
        if (useDistanceInstead)
        {
            // Distance-based spawning
            int maxChunkRange = Mathf.CeilToInt(spawnDistance / chunkSize);
            
            for (int x = -maxChunkRange; x <= maxChunkRange; x++)
            {
                for (int z = -maxChunkRange; z <= maxChunkRange; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                    Vector3 chunkWorldPos = GetChunkWorldPosition(chunkCoord);
                    
                    // Check if within distance
                    float distance = Vector3.Distance(player.position, chunkWorldPos);
                    if (distance <= spawnDistance)
                    {
                        chunksToKeep.Add(chunkCoord);
                        SpawnFogChunkIfNeeded(chunkCoord);
                    }
                }
            }
        }
        else
        {
            // Chunk count-based spawning (square around player)
            for (int x = -chunksAroundPlayer; x <= chunksAroundPlayer; x++)
            {
                for (int z = -chunksAroundPlayer; z <= chunksAroundPlayer; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                    chunksToKeep.Add(chunkCoord);
                    SpawnFogChunkIfNeeded(chunkCoord);
                }
            }
        }
        
        // Remove fog chunks that are too far
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var kvp in spawnedFogChunks)
        {
            if (!chunksToKeep.Contains(kvp.Key))
            {
                chunksToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var chunkCoord in chunksToRemove)
        {
            if (spawnedFogChunks.ContainsKey(chunkCoord))
            {
                Destroy(spawnedFogChunks[chunkCoord]);
                spawnedFogChunks.Remove(chunkCoord);
            }
        }
    }
    
    void SpawnFogChunkIfNeeded(Vector2Int chunkCoord)
    {
        // Skip if already spawned
        if (spawnedFogChunks.ContainsKey(chunkCoord))
            return;
        
        Vector3 spawnPosition = GetChunkWorldPosition(chunkCoord);
        
        // Height limits check
        maxTerrainHeight = terrain != null ? terrain.terrainData.size.y : 100f;
        var normalizedHeight = spawnPosition.y / maxTerrainHeight;
        if (normalizedHeight < minHeightLimit || normalizedHeight > maxHeightLimit)
            return;
        
        GameObject fogInstance = Instantiate(fogPrefab, spawnPosition, Quaternion.identity, transform);
        fogInstance.name = $"Fog_Chunk_{chunkCoord.x}_{chunkCoord.y}";
        
        spawnedFogChunks.Add(chunkCoord, fogInstance);
    }
    
    Vector2Int GetChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
    
    Vector3 GetChunkWorldPosition(Vector2Int chunkCoord)
    {
        var x = chunkCoord.x * chunkSize + chunkSize * 0.5f;
        var z = chunkCoord.y * chunkSize + chunkSize * 0.5f;
        
        // Get terrain height at this position
        float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) : 0f;
        
        // Apply offset
        y += heightOffset;
        
        return new Vector3(x, y, z);
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (player == null) return;
        
        Vector2Int playerChunk = GetChunkCoord(player.position);
        
        // Draw player chunk
        Gizmos.color = Color.green;
        Vector3 playerChunkPos = GetChunkWorldPosition(playerChunk);
        Gizmos.DrawWireCube(playerChunkPos, new Vector3(chunkSize, 1f, chunkSize));
        
        // Draw spawn range
        if (useDistanceInstead)
        {
            Gizmos.color = Color.yellow;
            DrawCircle(player.position, spawnDistance, 32);
        }
        else
        {
            // Draw chunk grid
            Gizmos.color = Color.cyan;
            for (int x = -chunksAroundPlayer; x <= chunksAroundPlayer; x++)
            {
                for (int z = -chunksAroundPlayer; z <= chunksAroundPlayer; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                    Vector3 chunkPos = GetChunkWorldPosition(chunkCoord);
                    Gizmos.DrawWireCube(chunkPos, new Vector3(chunkSize, 1f, chunkSize));
                }
            }
        }
        
        // Draw spawned fog chunks
        Gizmos.color = Color.blue;
        foreach (var kvp in spawnedFogChunks)
        {
            if (kvp.Value != null)
            {
                Gizmos.DrawSphere(kvp.Value.transform.position, 1f);
            }
        }
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
    
    // Clean up on disable
    void OnDisable()
    {
        // Destroy all spawned fog chunks
        foreach (var kvp in spawnedFogChunks)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        spawnedFogChunks.Clear();
        
        master.onPlayerMovedToNewChunk -= UpdateFogChunks;
    }
}