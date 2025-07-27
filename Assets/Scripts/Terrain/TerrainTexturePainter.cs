using System;
using UnityEngine;

public class TerrainTexturePainter : MonoBehaviour
{
    [Header("<b><color=#FFE066>Terrain Texture Painter</color></b>\n<i><color=#FFF2B2>Automatically paints terrain textures based on height values</color></i>")]
    
    [Header("Terrain References")]
    [SerializeField] private PerlinNoiseTerrainGenerator terrainGenerator;
    [SerializeField] private Terrain targetTerrain;
    
    [Header("Texture Settings")]
    [SerializeField] private TerrainTextureMode textureMode;
    [SerializeField] private TerrainTextureLayer[] textureLayers;
    
    private void Start()
    {
        // If texture mode is set to Default, skip the painting process
        if (textureMode == TerrainTextureMode.Default)
        {
            return;
        }
        
        // Auto-find components if not assigned
        if (terrainGenerator == null)
            terrainGenerator = FindFirstObjectByType<PerlinNoiseTerrainGenerator>();
            
        if (targetTerrain == null)
            targetTerrain = GetComponent<Terrain>();
        
        if (terrainGenerator == null || targetTerrain == null)
        {
            Debug.LogError("TerrainTexturePainter: Missing required components!");
            return;
        }
        
        // Setup terrain layers
        SetupTerrainLayers();
        
        // Initial paint
        //PaintTerrain();
    }
    
    private void SetupTerrainLayers()
    {
        if (textureLayers == null || textureLayers.Length == 0)
        {
            Debug.LogWarning("No texture layers defined!");
            return;
        }
        
        // Create TerrainLayer array for Unity's terrain system
        TerrainLayer[] unityTerrainLayers = new TerrainLayer[textureLayers.Length];
        
        for (int i = 0; i < textureLayers.Length; i++)
        {
            if (textureLayers[i].DiffuseTexture == null)
            {
                Debug.LogWarning($"Texture layer {i} has no diffuse texture assigned!");
                continue;
            }
            
            // Create new TerrainLayer
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = textureLayers[i].DiffuseTexture;
            layer.normalMapTexture = textureLayers[i].NormalTexture;
            layer.tileSize = textureLayers[i].TileSize;
            layer.tileOffset = textureLayers[i].TileOffset;
            
            unityTerrainLayers[i] = layer;
        }
        
        // Assign layers to terrain
        targetTerrain.terrainData.terrainLayers = unityTerrainLayers;
    }
    
    [ContextMenu("Paint Terrain")]
    public void PaintTerrain()
    {
        if (targetTerrain == null || terrainGenerator == null || textureLayers == null)
            return;
        
        TerrainData terrainData = targetTerrain.terrainData;
        
        // Use alphamap resolution, not heightmap resolution
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        
        // Get height data from terrain generator
        float[,] heights = terrainGenerator.GetTerrainHeights();
        int heightmapWidth = heights.GetLength(0);
        int heightmapHeight = heights.GetLength(1);
        
        // Create alphamap with correct dimensions
        float[,,] alphamap = new float[alphamapWidth, alphamapHeight, textureLayers.Length];
        
        // Calculate texture weights for each point
        for (int x = 0; x < alphamapWidth; x++)
        {
            for (int y = 0; y < alphamapHeight; y++)
            {
                // Map alphamap coordinates to heightmap coordinates
                int heightX = Mathf.RoundToInt((float)x * (heightmapWidth - 1) / (alphamapWidth - 1));
                int heightY = Mathf.RoundToInt((float)y * (heightmapHeight - 1) / (alphamapHeight - 1));
                
                // Clamp to ensure we don't go out of bounds
                heightX = Mathf.Clamp(heightX, 0, heightmapWidth - 1);
                heightY = Mathf.Clamp(heightY, 0, heightmapHeight - 1);
                
                float currentHeight = heights[heightX, heightY];
                float[] weights = CalculateTextureWeights(currentHeight);
                
                // Assign weights to alphamap
                for (int i = 0; i < textureLayers.Length && i < weights.Length; i++)
                {
                    alphamap[x, y, i] = weights[i];
                }
            }
        }
        
        // Apply the alphamap to terrain
        targetTerrain.terrainData.SetAlphamaps(0, 0, alphamap);
    }
    
    private float[] CalculateTextureWeights(float height)
    {
        // Create an array to hold weights for each texture layer
        float[] weights = new float[textureLayers.Length];
        
        // Find which texture layers should be applied at this height
        for (int i = 0; i < textureLayers.Length; i++)
        {
            // Get the current texture layer
            TerrainTextureLayer layer = textureLayers[i];
            
            if (height >= layer.MinHeight && height <= layer.MaxHeight)
            {
                // Calculate weight based on position within the layer's height range
                float layerRange = layer.MaxHeight - layer.MinHeight;
                
                if (layerRange > 0)
                {
                    // Calculate distance from center of layer
                    float layerCenter = (layer.MinHeight + layer.MaxHeight) / 2f;
                    float distanceFromCenter = Mathf.Abs(height - layerCenter);
                    float maxDistance = layerRange / 2f;
                    
                    // Calculate weight with falloff
                    float weight = 1f - (distanceFromCenter / maxDistance);
                    weight = Mathf.Pow(weight, layer.BlendSharpness);
                    weights[i] = Mathf.Clamp01(weight);
                }
                else
                {
                    // Single height value - full weight
                    weights[i] = 1f;
                }
            }
        }
        
        // Normalize weights so they sum to 1
        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            totalWeight += weights[i];
        }
        
        if (totalWeight > 0)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= totalWeight;
            }
        }
        else
        {
            // If no weights, default to first texture
            if (weights.Length > 0)
                weights[0] = 1f;
        }
        
        return weights;
    }
    
}

[Serializable]
public class TerrainTextureLayer
{
    [Header("Texture Assets")]
    public string LayerName = "New Layer";
    public Texture2D DiffuseTexture;
    public Texture2D NormalTexture;
    
    [Header("Height Range")]
    [Range(0f, 1f)]
    public float MinHeight = 0f;
    [Range(0f, 1f)]
    public float MaxHeight = 1f;
    
    [Header("Texture Tiling")]
    public Vector2 TileSize = Vector2.one * 15f;
    public Vector2 TileOffset = Vector2.zero;
    
    [Header("Blending")]
    [Range(0.1f, 5f)]
    public float BlendSharpness = 1f;  // Controls how sharp/soft the texture transitions are
}

public enum TerrainTextureMode
{
    Default,
    HeightBased
}