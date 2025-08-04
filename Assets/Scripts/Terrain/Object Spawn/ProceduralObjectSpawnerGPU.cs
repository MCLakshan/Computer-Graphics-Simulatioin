using UnityEngine;
using System.Collections.Generic;

// PROCEDURAL OBJECT GENERATION + GPU INSTANCING + FRUSTUM CULLING
public class ProceduralObjectSpawnerGPU : MonoBehaviour
{
    [Header("GPU Instancing Master")]
    [SerializeField] private ProceduralObjectSpawnerGPUMaster master; // Reference to the master script
    
    [Header("Terrain Settings")]
    public Terrain terrain;
    public Transform player;
    public Transform playerCamera;
    
    [Header("Object Mesh & Material")]
    public Mesh objectMesh;
    public Material objectMaterial; // MUST have "Enable GPU Instancing" checked!
    [SerializeField] float objectYOffset = -0.05f; // Offset to avoid z-fighting with terrain
    
    [Header("Blue Noise Settings")]
    public Texture2D blueNoiseTexture; // Your blue noise texture
    public float noiseScale = 0.1f;    // How zoomed in the noise is
    public float noiseThreshold = 0.3f; // Only spawn objects where noise > this value
    
    [Header("Object Distribution Settings")]
    [Range(0f, 1024f)]
    public float objectDensity = 100f;     // Objects per chunk (should be less than maxObjectsPerChunk)
    public float minObjectScale = 0.8f;
    public float maxObjectScale = 1.2f;
    public int chunksToRender = 10;       // How many chunks around player
    public float chunkSize = 32f;         // Size of each Objects chunk
    [Range(0f, 1f)]
    [SerializeField] float lowerBound = 0f; // Lower bound for Object placement
    [Range(0f, 1f)]
    [SerializeField] float upperBound = 1f; // Upper bound for Object placement
    
    [Header("Performance")]
    [Range(0, 1024)]
    public int maxObjectsPerChunk = 1000;   // Limit for performance (Should be less than 1024 because of GPU instancing limits)
    public float cullingDistance = 200f;  // Don't render Objects beyond this
    [Range(0f, 180f)]
    public float cameraFieldOfView = 100f; // Camera FOV for culling in degrees
    [SerializeField] private bool enableFrustumCulling = true; // Toggle for frustum culling
    
    // Debugging variables
    [Header("Debugging")]
    [SerializeField] private bool enableOnDrawGizmos = true; // Toggle for drawing gizmos
    
    private Vector2Int _lastPlayerChunk;
    private float maxTerrainHeight; // Max height of terrain for bounds checking
    private float cosHalfFOV;       // For the edge case of frustum culling (prevent crashes)
    
    // GPU Instancing data
    private Dictionary<Vector2Int, ObjectChunk> objectChunks;
    
    // Object chunk class
    public class ObjectChunk
    {
        public Vector2Int coordinate;
        public Matrix4x4[] transforms;
        public int objectCount;
        public bool isGenerated;
        public MaterialPropertyBlock propertyBlock; // Each chunk gets its own property block
        
        // Optimization: Use a flag to track if chunk is active
        // This can be used to quickly check if chunk should be rendered
        public bool isActive;
        
        public ObjectChunk(Vector2Int coord)
        {
            coordinate = coord;
            transforms = new Matrix4x4[0];
            objectCount = 0;
            isGenerated = false;
            propertyBlock = new MaterialPropertyBlock(); // Create property block for this chunk
            isActive = true; // Initially active
        }
    }
    
    void OnEnable()
    {
        master.onPlayerMovedToNewChunk += UpdateObjectChunks;
    }

    void OnDisable()
    {
        master.onPlayerMovedToNewChunk -= UpdateObjectChunks;
    }


    void Start()
    {
        cosHalfFOV = Mathf.Cos(cameraFieldOfView * 0.5f * Mathf.Deg2Rad);
        
        objectChunks = new Dictionary<Vector2Int, ObjectChunk>();
        
        // Validate setup
        if (objectMaterial != null && !objectMaterial.enableInstancing)
        {
            Debug.LogError("Object material MUST have 'Enable GPU Instancing' checked!");
        }
        
        if (blueNoiseTexture == null)
        {
            Debug.LogWarning("No blue noise texture assigned! Using random noise instead.");
        }
        
        maxTerrainHeight = terrain != null ? terrain.terrainData.size.y : 100f;
    }
    
    void Update()
    {
        /*
        // OPTIMIZATION: Only update chunks if player has moved to a new chunk
        var currentChunk = WorldToChunkCoord(player.position);
        if (_lastPlayerChunk != currentChunk)
        {
            UpdateObjectChunks();
            _lastPlayerChunk = currentChunk;
        }
        */
        
        RenderVisibleObjects();
    }
    
    
    private void UpdateObjectChunks()
    {
        if (player == null) return;
        
        Vector2Int playerChunk = WorldToChunkCoord(player.position);
        //Debug.Log($"Player is in chunk: {playerChunk}");
        
        // Generate chunks around player
        for (int x = -chunksToRender; x <= chunksToRender; x++)
        {
            for (int z = -chunksToRender; z <= chunksToRender; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                
                // Check if chunk should be loaded 
                // OPTIMIZATION: Use squared distance to avoid sqrt calculation for performance
                float sqrDistanceToChunk = (chunkCoord - playerChunk).sqrMagnitude * chunkSize * chunkSize;
                float sqrCullingDistance = cullingDistance * cullingDistance;

                if (sqrDistanceToChunk <= sqrCullingDistance)
                {
                    // Generate chunk if not exists
                    if (objectChunks.ContainsKey(chunkCoord))
                    {
                        // OPTIMIZATION: Reuse existing chunk if it exists
                        objectChunks[chunkCoord].isActive = true; // Mark as active
                    }
                    else
                    {
                        // Generate new chunk if it doesn't exist
                        GenerateObjectChunk(chunkCoord);
                    }
                }
                else
                {
                    if (objectChunks.ContainsKey(chunkCoord))
                    {
                        // OPTIMIZATION: Mark as inactive if within culling distance
                        // This allows us to skip rendering it if not in FOV
                        objectChunks[chunkCoord].isActive = false;
                    }
                }
            }
        }
    }
    
    Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
    
    Vector3 ChunkCoordToWorldPos(Vector2Int chunkCoord)
    {
        return new Vector3(
            chunkCoord.x * chunkSize,
            0,
            chunkCoord.y * chunkSize
        );
    }
    
    void GenerateObjectChunk(Vector2Int chunkCoord)
    {
        ObjectChunk newChunk = new ObjectChunk(chunkCoord);
        Vector3 chunkWorldPos = ChunkCoordToWorldPos(chunkCoord);
        
        List<Matrix4x4> chunkTransforms = new List<Matrix4x4>();
        
        // Generate Object positions within chunk
        for (int i = 0; i < objectDensity && chunkTransforms.Count < maxObjectsPerChunk; i++)
        {
            // Random position within chunk
            Vector3 localPos = new Vector3(
                Random.Range(0, chunkSize),
                0,
                Random.Range(0, chunkSize)
            );
            
            Vector3 worldPos = chunkWorldPos + localPos;
            
            // Check if this position should have Objects using blue noise + terrain data
            if (ShouldPlaceObjectAt(worldPos))
            {
                // Sample terrain height
                float terrainHeight = SampleTerrainHeight(worldPos);
                worldPos.y = terrainHeight;
                
                // Check if within bounds
                if (!IsWithinBounds(worldPos.y))
                {
                    continue;
                }
                
                // Apply slight offset to avoid z-fighting
                worldPos.y += objectYOffset;
                
                // Get terrain slope (don't place Objects on steep slopes)
                float slope = GetTerrainSlope(worldPos);
                if (slope > 45f) continue; // Skip steep areas
                
                // Create Object transform
                Quaternion rotation = Quaternion.Euler(
                    Random.Range(-5f, 5f),  // Slight tilt
                    Random.Range(0, 360f),  // Random rotation
                    Random.Range(-5f, 5f)   // Slight tilt
                );
                
                Vector3 scale = Vector3.one * Random.Range(minObjectScale, maxObjectScale);
                Matrix4x4 objectTransform = Matrix4x4.TRS(worldPos, rotation, scale);
                chunkTransforms.Add(objectTransform);
            }
        }
        
        // Store in chunk
        newChunk.transforms = chunkTransforms.ToArray();
        newChunk.objectCount = chunkTransforms.Count;
        newChunk.isGenerated = true;
        
        objectChunks.Add(chunkCoord, newChunk);
        
        // Debug.Log($"Generated Objects chunk {chunkCoord} with {newChunk.objectCount} Objects");
    }
    
    bool ShouldPlaceObjectAt(Vector3 worldPos)
    {
        if (blueNoiseTexture == null)
        {
            // Fallback to random if no blue noise
            return Random.value > noiseThreshold;
        }
        
        // Sample blue noise texture
        Vector2 noiseUV = new Vector2(
            (worldPos.x * noiseScale) % 1f,
            (worldPos.z * noiseScale) % 1f
        );
        
        // Make sure UV is positive
        if (noiseUV.x < 0) noiseUV.x += 1f;
        if (noiseUV.y < 0) noiseUV.y += 1f;
        
        // Sample the blue noise texture
        Color noiseValue = blueNoiseTexture.GetPixelBilinear(noiseUV.x, noiseUV.y);
        
        // Use red channel for Object distribution
        return noiseValue.r > noiseThreshold;
    }
    
    float SampleTerrainHeight(Vector3 worldPos)
    {
        if (terrain == null) return 0f;
        return terrain.SampleHeight(worldPos);
    }
    
    float GetTerrainSlope(Vector3 worldPos)
    {
        if (terrain == null) return 0f;
        
        // Convert world position to terrain coordinates (0-1)
        Vector3 terrainPos = worldPos - terrain.transform.position;
        Vector2 normalizedPos = new Vector2(
            terrainPos.x / terrain.terrainData.size.x,
            terrainPos.z / terrain.terrainData.size.z
        );
        
        // Clamp to terrain bounds
        normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
        normalizedPos.y = Mathf.Clamp01(normalizedPos.y);
        
        return terrain.terrainData.GetSteepness(normalizedPos.x, normalizedPos.y);
    }
    
    private void RenderVisibleObjects()
    {
        if (player == null) return;
        
        Vector3 playerPos = player.position;
        var playerChunk = WorldToChunkCoord(playerPos);
        
        foreach (var kvp in objectChunks)
        {
            ObjectChunk chunk = kvp.Value;
            // OPTIMIZATION: Skip chunks that are not generated, have no Objects, or are inactive
            if (!chunk.isGenerated || chunk.objectCount == 0 || !chunk.isActive) continue;
            
            // Always render the player's chunk and its 8 neighbors
            if (chunk.coordinate == playerChunk ||
                Mathf.Abs(chunk.coordinate.x - playerChunk.x) <= 1 &&
                Mathf.Abs(chunk.coordinate.y - playerChunk.y) <= 1)
            {
                Graphics.DrawMeshInstanced(
                    objectMesh,
                    0,
                    objectMaterial,
                    chunk.transforms,
                    chunk.objectCount,
                    null    // Use null for property block if not needed (cuz the material already has colors set)
                );
                continue; // Skip further checks for player's chunk
            }
            
            // Check if chunk is close enough to render
            Vector3 chunkCenter = ChunkCoordToWorldPos(chunk.coordinate) + Vector3.one * chunkSize * 0.5f;
            float distanceToPlayer = Vector3.Distance(playerPos, chunkCenter);
            
            if (distanceToPlayer <= cullingDistance && AngleBasedFrustumCullingAngleCheck(chunkCenter) && enableFrustumCulling)
            {
                // Render this chunk if within culling distance and FOV
                Graphics.DrawMeshInstanced(
                    objectMesh,
                    0,
                    objectMaterial,
                    chunk.transforms,
                    chunk.objectCount,
                    null    // Use null for property block if not needed (cuz the material already has colors set)
                );
            }
            else if (!enableFrustumCulling) 
            {
                // If frustum culling is disabled, render all chunks
                Graphics.DrawMeshInstanced(
                    objectMesh,
                    0,
                    objectMaterial,
                    chunk.transforms,
                    chunk.objectCount,
                    null    // Use null for property block if not needed (cuz the material already has colors set)
                );
            }
        }
    }
    
    private bool IsWithinBounds(float height)
    {
        var normalizedHeight = height / maxTerrainHeight;
        return normalizedHeight >= lowerBound && normalizedHeight <= upperBound;
    }

    private bool AngleBasedFrustumCullingAngleCheck(Vector3 chunkPosition)
    {
        Vector3 camaraForwardDirection = playerCamera.forward;
        Vector2 camaraForwardDirection2D = new Vector2(camaraForwardDirection.x, camaraForwardDirection.z).normalized;
        Vector3 camaraPosition = playerCamera.position;
        Vector2 camaraPosition2D = new Vector2(camaraPosition.x, camaraPosition.z);
        Vector2 chunkPosition2D = new Vector2(chunkPosition.x, chunkPosition.z);
        Vector2 directionToChunk = (chunkPosition2D - camaraPosition2D).normalized;
        float dotProduct = Vector2.Dot(camaraForwardDirection2D, directionToChunk);
        return dotProduct > cosHalfFOV; // Check if chunk is within playerCamera FOV
    }
    
    
    // On Draw Gizmos, visualize Object chunks - useful for debugging ---------------------------------------------------
    
    void OnDrawGizmos()
    {
        if (!enableOnDrawGizmos) return; // Skip if gizmos are disabled
        if (playerCamera == null) return;
        
        // Set gizmo color for FOV visualization
        Gizmos.color = Color.yellow;
        
        Vector3 cameraPos = playerCamera.position;
        Vector3 cameraForward = playerCamera.forward;
        
        // Calculate the FOV angle in radians
        float halfFOVRadians = cameraFieldOfView * 0.5f * Mathf.Deg2Rad;
        
        // Calculate the left and right directions for the FOV cone
        Vector3 rightDirection = Quaternion.AngleAxis(cameraFieldOfView * 0.5f, Vector3.up) * cameraForward;
        Vector3 leftDirection = Quaternion.AngleAxis(-cameraFieldOfView * 0.5f, Vector3.up) * cameraForward;
        
        // Draw the FOV cone lines
        float fovDistance = cullingDistance; // Use culling distance as the FOV visualization range
        
        // Draw center line (playerCamera forward direction)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(cameraPos, cameraPos + cameraForward * fovDistance);
        
        // Draw left and right FOV boundary lines
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cameraPos, cameraPos + leftDirection * fovDistance);
        Gizmos.DrawLine(cameraPos, cameraPos + rightDirection * fovDistance);
        
        // Draw the arc at the end of the FOV
        Vector3 leftEndPoint = cameraPos + leftDirection * fovDistance;
        Vector3 rightEndPoint = cameraPos + rightDirection * fovDistance;
        Gizmos.DrawLine(leftEndPoint, rightEndPoint);
        
        // Optional: Draw FOV arc with multiple segments for smoother visualization
        Gizmos.color = Color.cyan;
        int arcSegments = 20;
        Vector3 previousPoint = leftEndPoint;
        
        for (int i = 1; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(-cameraFieldOfView * 0.5f, cameraFieldOfView * 0.5f, (float)i / arcSegments);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * cameraForward;
            Vector3 currentPoint = cameraPos + direction * fovDistance;
            
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        
        // Visualize chunk centers and their culling status
        if (objectChunks != null && player != null)
        {
            Vector2Int playerChunk = WorldToChunkCoord(player.position);
            
            foreach (var kvp in objectChunks)
            {
                ObjectChunk chunk = kvp.Value;
                if (!chunk.isGenerated) continue;
                
                Vector3 chunkCenter = ChunkCoordToWorldPos(chunk.coordinate) + Vector3.one * chunkSize * 0.5f;
                
                // Different colors based on culling status
                if (chunk.coordinate == playerChunk || 
                    (Mathf.Abs(chunk.coordinate.x - playerChunk.x) <= 1 &&
                     Mathf.Abs(chunk.coordinate.y - playerChunk.y) <= 1))
                {
                    // Player's chunk - always rendered (green)
                    // And also its immediate neighbors
                    Gizmos.color = Color.green;
                }
                else if (Vector3.Distance(player.position, chunkCenter) > cullingDistance)
                {
                    // Too far - not rendered (red)
                    Gizmos.color = Color.red;
                }
                else if (AngleBasedFrustumCullingAngleCheck(chunkCenter))
                {
                    // Within FOV - rendered (blue)
                    Gizmos.color = Color.blue;
                }
                else
                {
                    // Outside FOV - not rendered (orange)
                    Gizmos.color = Color.magenta;
                }
                
                // Draw chunk center as a sphere
                Gizmos.DrawSphere(chunkCenter, 2f);
                
                // Draw chunk bounds as wireframe cube
                Gizmos.color = Color.white * 0.3f; // Semi-transparent white
                Gizmos.DrawWireCube(chunkCenter, new Vector3(chunkSize, 10f, chunkSize));
            }
        }
        
        // Draw player position
        if (player != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(player.position, 3f);
            
            // Draw culling distance circle around player
            Gizmos.color = Color.white * 0.5f;
            DrawCircle(player.position, cullingDistance, 32);
        }
    }

    // Helper function to draw a circle
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
}