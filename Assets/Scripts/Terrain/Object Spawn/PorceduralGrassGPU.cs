using UnityEngine;
using System.Collections.Generic;

// PROCEDURAL GRASS GENERATION + GPU INSTANCING
public class ProceduralGrassGPU : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Terrain terrain;
    public Transform player;
    
    [Header("Grass Mesh & Material")]
    public Mesh grassMesh;
    public Material grassMaterial; // MUST have "Enable GPU Instancing" checked!
    
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
    
    [Header("Performance")]
    public int maxGrassPerChunk = 1000;   // Limit for performance
    public float cullingDistance = 200f;  // Don't render grass beyond this
    
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
        
        public GrassChunk(Vector2Int coord)
        {
            coordinate = coord;
            transforms = new Matrix4x4[0];
            colors = new Vector4[0];
            grassCount = 0;
            isGenerated = false;
            propertyBlock = new MaterialPropertyBlock(); // Create property block for this chunk
        }
    }
    
    void Start()
    {
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
    }
    
    void Update()
    {
        UpdateGrassChunks();
        RenderVisibleGrass();
    }
    
    void UpdateGrassChunks()
    {
        if (player == null) return;
        
        Vector2Int playerChunk = WorldToChunkCoord(player.position);
        Debug.Log($"Player is in chunk: {playerChunk}");
        
        // Generate chunks around player
        for (int x = -chunksToRender; x <= chunksToRender; x++)
        {
            for (int z = -chunksToRender; z <= chunksToRender; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                
                // Check if chunk should be loaded
                float distanceToChunk = Vector2.Distance(playerChunk, chunkCoord) * chunkSize;
                
                if (distanceToChunk <= cullingDistance)
                {
                    // Generate chunk if not exists
                    if (!grassChunks.ContainsKey(chunkCoord))
                    {
                        GenerateGrassChunk(chunkCoord);
                    }
                }
                else
                {
                    // Remove distant chunks to save memory
                    if (grassChunks.ContainsKey(chunkCoord))
                    {
                        grassChunks.Remove(chunkCoord);
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
        
        Debug.Log($"Generated grass chunk {chunkCoord} with {newChunk.grassCount} grass blades");
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
        
        foreach (var kvp in grassChunks)
        {
            GrassChunk chunk = kvp.Value;
            if (!chunk.isGenerated || chunk.grassCount == 0) continue;
            
            // Check if chunk is close enough to render
            Vector3 chunkCenter = ChunkCoordToWorldPos(chunk.coordinate) + Vector3.one * chunkSize * 0.5f;
            float distanceToPlayer = Vector3.Distance(playerPos, chunkCenter);
            
            if (distanceToPlayer <= cullingDistance)
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
            }
        }
    }
    
    // Debug info
    void OnGUI()
    {
        if (Application.isPlaying)
        {
            int totalGrass = 0;
            int visibleChunks = 0;
            
            foreach (var chunk in grassChunks.Values)
            {
                totalGrass += chunk.grassCount;
                visibleChunks++;
            }
            
            GUI.Label(new Rect(10, 10, 300, 20), $"Grass Chunks: {visibleChunks}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Total Grass Blades: {totalGrass}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Draw Calls: {visibleChunks} (instead of {totalGrass})");
        }
    }
}