using UnityEngine;
using System.Collections.Generic;

// PROCEDURAL GRASS GENERATION + GPU INSTANCING
public class ProceduralGrassGPU : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;
    public Transform player;
    public Transform camera;
    
    [Header("Grass Mesh & Material")]
    public Mesh grassMesh;
    public Material grassMaterial; // MUST have "Enable GPU Instancing" checked!
    [SerializeField] float grassYOffset = 0.1f; // Offset to avoid z-fighting with terrain
    
    [Header("Blue Noise Settings")]
    public Texture2D blueNoiseTexture; // Your blue noise texture
    public float noiseScale = 0.1f;    // How zoomed in the noise is
    public float noiseThreshold = 0.3f; // Only spawn grass where noise > this value
    
    [Header("Grass Distribution")]
    public float grassDensity = 100f;     // Grass blades per chunk
    public float minGrassScale = 0.8f;
    public float maxGrassScale = 1.2f;
    public int chunksToRender = 10;       // How many chunks around player
    public float chunkSize = 50f;         // Size of each grass chunk
    [Range(0f, 1f)]
    [SerializeField] float lowerBound = 0f; // Lower bound for grass placement
    [Range(0f, 1f)]
    [SerializeField] float upperBound = 1f; // Upper bound for grass placement
    
    [Header("Performance")]
    public int maxGrassPerChunk = 1000;   // Limit for performance
    public float cullingDistance = 200f;  // Don't render grass beyond this
    public float cameraFieldOfView = 60f; // Camera FOV for culling in degrees
    
    // Debugging variables
    [Header("Debugging")]
    [SerializeField] private bool enableOnDrawGizmos = true; // Toggle for drawing gizmos
    [SerializeField] private bool enableFrustumCulling = true; // Toggle for frustum culling
    
    private Vector2Int _lastPlayerChunk;
    private float maxTerrainHeight; // Max height of terrain for bounds checking
    private float cosHalfFOV;       // For the edge case of frustum culling (prevent crashes)
    
    // GPU Instancing data
    private Dictionary<Vector2Int, GrassChunk> grassChunks;
    
    // Grass chunk class
    public class GrassChunk
    {
        public Vector2Int coordinate;
        public Matrix4x4[] transforms;
        public Vector4[] colors;
        public int grassCount;
        public bool isGenerated;
        public MaterialPropertyBlock propertyBlock; // Each chunk gets its own property block
        
        // Optimization: Use a flag to track if chunk is active
        // This can be used to quickly check if chunk should be rendered
        public bool isActive;
        
        public GrassChunk(Vector2Int coord)
        {
            coordinate = coord;
            transforms = new Matrix4x4[0];
            colors = new Vector4[0];
            grassCount = 0;
            isGenerated = false;
            propertyBlock = new MaterialPropertyBlock(); // Create property block for this chunk
            isActive = true; // Initially active
        }
    }
    
    void Start()
    {
        cosHalfFOV = Mathf.Cos(cameraFieldOfView * 0.5f * Mathf.Deg2Rad);
        
        grassChunks = new Dictionary<Vector2Int, GrassChunk>();
        
        // Validate setup
        if (grassMaterial != null && !grassMaterial.enableInstancing)
        {
            Debug.LogError("Grass material MUST have 'Enable GPU Instancing' checked!");
        }
        
        if (blueNoiseTexture == null)
        {
            Debug.LogWarning("No blue noise texture assigned! Using random noise instead.");
        }
        
        maxTerrainHeight = terrain != null ? terrain.terrainData.size.y : 100f;
    }
    
    void Update()
    {
        
        // OPTIMIZATION: Only update chunks if player has moved to a new chunk
        var currentChunk = WorldToChunkCoord(player.position);
        if (_lastPlayerChunk != currentChunk)
        {
            UpdateGrassChunks();
            _lastPlayerChunk = currentChunk;
        }
        
        RenderVisibleGrass();
    }
    
    void UpdateGrassChunks()
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
                    if (grassChunks.ContainsKey(chunkCoord))
                    {
                        // OPTIMIZATION: Reuse existing chunk if it exists
                        grassChunks[chunkCoord].isActive = true; // Mark as active
                    }
                    else
                    {
                        // Generate new chunk if it doesn't exist
                        GenerateGrassChunk(chunkCoord);
                    }
                }
                else
                {
                    if (grassChunks.ContainsKey(chunkCoord))
                    {
                        // OPTIMIZATION: Mark as inactive if within culling distance
                        // This allows us to skip rendering it if not in FOV
                        grassChunks[chunkCoord].isActive = false;
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
    
    void GenerateGrassChunk(Vector2Int chunkCoord)
    {
        GrassChunk newChunk = new GrassChunk(chunkCoord);
        Vector3 chunkWorldPos = ChunkCoordToWorldPos(chunkCoord);
        
        List<Matrix4x4> chunkTransforms = new List<Matrix4x4>();
        List<Vector4> chunkColors = new List<Vector4>();
        
        // Generate grass positions within chunk
        for (int i = 0; i < grassDensity && chunkTransforms.Count < maxGrassPerChunk; i++)
        {
            // Random position within chunk
            Vector3 localPos = new Vector3(
                Random.Range(0, chunkSize),
                0,
                Random.Range(0, chunkSize)
            );
            
            Vector3 worldPos = chunkWorldPos + localPos;
            
            // Check if this position should have grass using blue noise + terrain data
            if (ShouldPlaceGrassAt(worldPos))
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
                worldPos.y += grassYOffset;
                
                // Get terrain slope (don't place grass on steep slopes)
                float slope = GetTerrainSlope(worldPos);
                if (slope > 45f) continue; // Skip steep areas
                
                // Check terrain texture (optional - don't place on rocks/water)
                if (!IsGrassCompatibleTerrain(worldPos)) continue;
                
                // Create grass transform
                Quaternion rotation = Quaternion.Euler(
                    Random.Range(-5f, 5f),  // Slight tilt
                    Random.Range(0, 360f),  // Random rotation
                    Random.Range(-5f, 5f)   // Slight tilt
                );
                
                Vector3 scale = Vector3.one * Random.Range(minGrassScale, maxGrassScale);
                Matrix4x4 grassTransform = Matrix4x4.TRS(worldPos, rotation, scale);
                
                // Vary grass color based on position/noise
                Vector4 grassColor = GetGrassColor(worldPos);
                
                chunkTransforms.Add(grassTransform);
                chunkColors.Add(grassColor);
            }
        }
        
        // Store in chunk
        newChunk.transforms = chunkTransforms.ToArray();
        newChunk.colors = chunkColors.ToArray();
        newChunk.grassCount = chunkTransforms.Count;
        newChunk.isGenerated = true;
        
        // Set the colors in the chunk's property block once when generating
        if (newChunk.grassCount > 0)
        {
            newChunk.propertyBlock.SetVectorArray("_Colors", newChunk.colors);
        }
        
        grassChunks.Add(chunkCoord, newChunk);
        
        // Debug.Log($"Generated grass chunk {chunkCoord} with {newChunk.grassCount} grass blades");
    }
    
    bool ShouldPlaceGrassAt(Vector3 worldPos)
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
        
        // Use red channel for grass distribution
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
    
    bool IsGrassCompatibleTerrain(Vector3 worldPos)
    {
        // Optional: Check terrain texture layers
        // You can sample terrain textures to avoid placing grass on rocks, water, etc.
        
        if (terrain == null) return true;
        
        // Example: Check if position is on grass texture
        Vector3 terrainPos = worldPos - terrain.transform.position;
        Vector2 normalizedPos = new Vector2(
            terrainPos.x / terrain.terrainData.size.x,
            terrainPos.z / terrain.terrainData.size.z
        );
        
        // Clamp to terrain bounds
        normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
        normalizedPos.y = Mathf.Clamp01(normalizedPos.y);
        
        // Sample terrain texture weights (if you have multiple terrain textures)
        // float[,,] alphamaps = terrain.terrainData.GetAlphamaps(
        //     Mathf.FloorToInt(normalizedPos.x * terrain.terrainData.alphamapWidth),
        //     Mathf.FloorToInt(normalizedPos.y * terrain.terrainData.alphamapHeight),
        //     1, 1
        // );
        // 
        // // Check if grass texture (index 0) is dominant
        // return alphamaps[0, 0, 0] > 0.5f; // Grass texture weight > 50%
        
        return true; // For now, allow grass everywhere
    }
    
    Vector4 GetGrassColor(Vector3 worldPos)
    {
        // Vary grass color based on position for natural look
        float noise = Mathf.PerlinNoise(worldPos.x * 0.02f, worldPos.z * 0.02f);
        
        // Base green color with variation
        float greenness = 0.8f + noise * 0.2f;
        
        return new Vector4(
            0.3f + noise * 0.2f,  // Red (slight brown tint)
            greenness,            // Green (dominant)
            0.2f + noise * 0.1f,  // Blue 
            1.0f                  // Alpha
        );
    }
    
    void RenderVisibleGrass()
    {
        if (player == null) return;
        
        Vector3 playerPos = player.position;
        var playerChunk = WorldToChunkCoord(playerPos);
        
        foreach (var kvp in grassChunks)
        {
            GrassChunk chunk = kvp.Value;
            // OPTIMIZATION: Skip chunks that are not generated, have no grass, or are inactive
            if (!chunk.isGenerated || chunk.grassCount == 0 || !chunk.isActive) continue;
            
            // Always render the player's chunk and its 8 neighbors
            if (chunk.coordinate == playerChunk ||
                Mathf.Abs(chunk.coordinate.x - playerChunk.x) <= 1 &&
                Mathf.Abs(chunk.coordinate.y - playerChunk.y) <= 1)
            {
                // Use the chunk's own property block (colors already set during generation)
                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    chunk.transforms,
                    chunk.grassCount,
                    chunk.propertyBlock
                );
                continue; // Skip further checks for player's chunk
            }
            
            // Check if chunk is close enough to render
            Vector3 chunkCenter = ChunkCoordToWorldPos(chunk.coordinate) + Vector3.one * chunkSize * 0.5f;
            float distanceToPlayer = Vector3.Distance(playerPos, chunkCenter);
            
            if (distanceToPlayer <= cullingDistance && AngleBasedFrustumCullingAngleCheck(chunkCenter) && enableFrustumCulling)
            {
                // Render this chunk if within culling distance and FOV
                // Use the chunk's own property block (colors already set during generation)
                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    chunk.transforms,
                    chunk.grassCount,
                    chunk.propertyBlock
                );
            }
            else if (!enableFrustumCulling) 
            {
                // If frustum culling is disabled, render all chunks
                // Use the chunk's own property block (colors already set during generation)
                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    chunk.transforms,
                    chunk.grassCount,
                    chunk.propertyBlock
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
        Vector3 camaraForwardDirection = camera.forward;
        Vector2 camaraForwardDirection2D = new Vector2(camaraForwardDirection.x, camaraForwardDirection.z).normalized;
        Vector3 camaraPosition = camera.position;
        Vector2 camaraPosition2D = new Vector2(camaraPosition.x, camaraPosition.z);
        Vector2 chunkPosition2D = new Vector2(chunkPosition.x, chunkPosition.z);
        Vector2 directionToChunk = (chunkPosition2D - camaraPosition2D).normalized;
        float dotProduct = Vector2.Dot(camaraForwardDirection2D, directionToChunk);
        return dotProduct > cosHalfFOV; // Check if chunk is within camera FOV
    }
    
    
    // On Draw Gizmos, visualize grass chunks - useful for debugging ---------------------------------------------------
    
    void OnDrawGizmos()
    {
        if (!enableOnDrawGizmos) return; // Skip if gizmos are disabled
        if (camera == null) return;
        
        // Set gizmo color for FOV visualization
        Gizmos.color = Color.yellow;
        
        Vector3 cameraPos = camera.position;
        Vector3 cameraForward = camera.forward;
        
        // Calculate the FOV angle in radians
        float halfFOVRadians = cameraFieldOfView * 0.5f * Mathf.Deg2Rad;
        
        // Calculate the left and right directions for the FOV cone
        Vector3 rightDirection = Quaternion.AngleAxis(cameraFieldOfView * 0.5f, Vector3.up) * cameraForward;
        Vector3 leftDirection = Quaternion.AngleAxis(-cameraFieldOfView * 0.5f, Vector3.up) * cameraForward;
        
        // Draw the FOV cone lines
        float fovDistance = cullingDistance; // Use culling distance as the FOV visualization range
        
        // Draw center line (camera forward direction)
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
        if (grassChunks != null && player != null)
        {
            Vector2Int playerChunk = WorldToChunkCoord(player.position);
            
            foreach (var kvp in grassChunks)
            {
                GrassChunk chunk = kvp.Value;
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