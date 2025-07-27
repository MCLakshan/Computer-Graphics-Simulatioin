using UnityEngine;
using UnityEngine.UI;

public class TextureGenerator_Dirt : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    
    [Header("Soil Base - Controls overall dirt appearance")]
    [Range(5f, 50f)]
    [SerializeField] private float soilScale = 15f;          // Base soil pattern frequency
    [Range(0f, 1f)]
    [SerializeField] private float soilContrast = 0.6f;      // How varied the base soil looks
    
    [Header("Rock/Pebble Details - Adds realistic rocky elements")]
    [SerializeField] private int pebbleCount = 800;          // Number of pebbles/rocks
    [Range(0.5f, 8f)]
    [SerializeField] private float pebbleSize = 2f;          // Size of individual pebbles
    [Range(0.1f, 3f)]
    [SerializeField] private float pebbleSharpness = 1.2f;   // How defined pebble edges are
    
    [Header("Fine Dirt Particles - Creates grainy soil texture")]
    [Range(20f, 100f)]
    [SerializeField] private float particleScale = 40f;      // Frequency of dirt particles
    [Range(0f, 0.5f)]
    [SerializeField] private float particleStrength = 0.3f;  // How prominent particles are
    
    [Header("Moisture/Wetness - Adds realistic wet/dry variation")]
    [Range(5f, 30f)]
    [SerializeField] private float moistureScale = 12f;      // Size of wet/dry patches
    [Range(0f, 1f)]
    [SerializeField] private float moistureAmount = 0.4f;    // Overall moisture level
    
    [Header("Organic Matter - Adds decomposed leaves/twigs")]
    [SerializeField] private int organicCount = 200;         // Number of organic bits
    [Range(1f, 10f)]
    [SerializeField] private float organicSize = 3f;         // Size of organic matter
    [Range(0f, 1f)]
    [SerializeField] private float organicDensity = 0.3f;    // How much organic matter
    
    [Header("Dirt Colors - Realistic soil color palette")]
    [SerializeField] private Color lightDirtColor = new Color(0.7f, 0.5f, 0.3f, 1f);    // Light dry dirt
    [SerializeField] private Color darkDirtColor = new Color(0.4f, 0.3f, 0.2f, 1f);     // Dark dry dirt
    [SerializeField] private Color wetDirtColor = new Color(0.25f, 0.2f, 0.15f, 1f);    // Wet/moist dirt
    [SerializeField] private Color organicColor = new Color(0.15f, 0.1f, 0.05f, 1f);    // Decomposed organic matter
    [SerializeField] private Color pebbleColor = new Color(0.6f, 0.55f, 0.5f, 1f);      // Rock/pebble color
    
    [Header("Visualization")]
    [SerializeField] private RawImage uiRawImage;          // For UI display
    [SerializeField] private bool applyToMaterial = true;  // Apply to GameObject's material
    
    // Private working data
    private Vector2[] pebbleCenters;     // Positions of pebbles/rocks
    private Vector2[] organicCenters;    // Positions of organic matter
    private Texture2D generatedTexture; // The final dirt texture
    
    void Start()
    {
        Debug.Log("Dirt Texture Generator Started!");
        
        // Generate random positions for texture elements
        pebbleCenters = GenerateRandomCenters(pebbleCount, textureWidth, textureHeight);
        organicCenters = GenerateRandomCenters(organicCount, textureWidth, textureHeight);
        
        // Create and generate the texture
        CreateTexture();
        GenerateDirtTexture();
        
        Debug.Log($"Dirt texture generated: {textureWidth}x{textureHeight} with {pebbleCount} pebbles and {organicCount} organic elements");
    }
    
    // Create the texture object with proper settings
    private void CreateTexture()
    {
        // Clean up existing texture
        if (generatedTexture != null)
        {
            DestroyImmediate(generatedTexture);
        }
        
        // Create new texture with appropriate format for dirt
        generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        generatedTexture.name = "Generated_Dirt_Texture";
        generatedTexture.filterMode = FilterMode.Bilinear; // Smooth filtering for natural look
        generatedTexture.wrapMode = TextureWrapMode.Repeat; // Allow seamless tiling
        
        Debug.Log("Dirt texture object created");
    }
    
    // Main function that generates the complete dirt texture
    private void GenerateDirtTexture()
    {
        Debug.Log("Generating dirt texture layers...");
        
        // Create array to store all pixel colors
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        // Generate each pixel's color using our layered approach
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                // Calculate the array index for this pixel
                int pixelIndex = y * textureWidth + x;
                
                // Calculate the color for this pixel using our dirt algorithm
                pixels[pixelIndex] = CalculateDirtPixelColor(x, y);
            }
        }
        
        // Apply all pixels to the texture at once (efficient)
        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply(); // Actually update the GPU texture
        
        // Display the texture
        DisplayTexture();
        
        Debug.Log("Dirt texture generation complete!");
    }
    
    // Display the generated texture on UI and/or material
    private void DisplayTexture()
    {
        // Apply to RawImage UI component if assigned
        if (uiRawImage != null)
        {
            uiRawImage.texture = generatedTexture;
            Debug.Log("Dirt texture applied to RawImage UI");
        }
        
        // Apply to GameObject's material if enabled
        if (applyToMaterial)
        {
            Renderer objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectRenderer.material.mainTexture = generatedTexture;
                Debug.Log("Dirt texture applied to GameObject material");
            }
        }
    }
    
    // Generate random positions for texture elements (pebbles, organic matter, etc.)
    private Vector2[] GenerateRandomCenters(int count, int width, int height)
    {
        Vector2[] centers = new Vector2[count];
        
        for (int i = 0; i < count; i++)
        {
            // Random position within texture bounds
            centers[i] = new Vector2(
                Random.Range(0f, width),   // X position
                Random.Range(0f, height)  // Y position
            );
        }
        
        return centers;
    }
    
    // Calculate base soil pattern using layered Perlin noise
    // This creates the fundamental dirt structure with natural variation
    private float CalculateBaseSoilPattern(int x, int y)
    {
        // Convert pixel coordinates to noise coordinates
        float xCoord = (float)x / textureWidth * soilScale;
        float yCoord = (float)y / textureHeight * soilScale;
        
        // Layer 1: Base soil structure (large patches)
        float baseLayer = Mathf.PerlinNoise(xCoord, yCoord);
        
        // Layer 2: Medium soil variation
        float mediumLayer = Mathf.PerlinNoise(xCoord * 2f, yCoord * 2f) * 0.5f;
        
        // Layer 3: Fine soil detail
        float fineLayer = Mathf.PerlinNoise(xCoord * 4f, yCoord * 4f) * 0.25f;
        
        // Combine layers
        float soilValue = baseLayer + mediumLayer + fineLayer;
        
        // Apply contrast to make soil patches more defined
        soilValue = Mathf.Pow(Mathf.Clamp01(soilValue / 1.75f), 1f + soilContrast);
        
        return soilValue;
    }
    
    // Calculate pebble/rock contribution using Voronoi diagram
    // This creates scattered rocks and pebbles throughout the dirt
    private float CalculatePebbleContribution(int x, int y)
    {
        if (pebbleCenters.Length == 0) return 0f;
        
        float minDistance = float.MaxValue;
        Vector2 currentPixel = new Vector2(x, y);
        
        // Find distance to closest pebble center
        foreach (Vector2 center in pebbleCenters)
        {
            float distance = Vector2.Distance(currentPixel, center);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        // Convert distance to pebble intensity
        float normalizedDistance = minDistance / (pebbleSize * 8f);
        float pebbleIntensity = 1f - Mathf.Clamp01(normalizedDistance);
        
        // Apply sharpness to create defined pebble edges
        pebbleIntensity = Mathf.Pow(pebbleIntensity, pebbleSharpness);
        
        return pebbleIntensity;
    }
    
    // Calculate fine dirt particles using high-frequency noise
    // This adds the grainy, sandy texture that makes dirt look realistic
    private float CalculateFineParticles(int x, int y)
    {
        // Convert pixel coordinates to high-frequency noise coordinates
        float xCoord = (float)x / textureWidth * particleScale;
        float yCoord = (float)y / textureHeight * particleScale;
        
        // Generate fine particle noise
        float particleNoise = Mathf.PerlinNoise(xCoord, yCoord);
        
        // Center around 0 and apply strength
        float particleDetail = (particleNoise - 0.5f) * particleStrength;
        
        return particleDetail;
    }
    
    // Calculate moisture/wetness variation across the texture
    // This creates realistic wet and dry patches in the dirt
    private float CalculateMoistureLevel(int x, int y)
    {
        // Convert pixel coordinates to moisture noise coordinates
        float xCoord = (float)x / textureWidth * moistureScale;
        float yCoord = (float)y / textureHeight * moistureScale;
        
        // Generate moisture pattern with multiple octaves for realism
        float moistureBase = Mathf.PerlinNoise(xCoord, yCoord);
        float moistureDetail = Mathf.PerlinNoise(xCoord * 2f, yCoord * 2f) * 0.5f;
        
        // Combine and scale by overall moisture amount
        float moistureValue = (moistureBase + moistureDetail) / 1.5f;
        moistureValue *= moistureAmount;
        
        return Mathf.Clamp01(moistureValue);
    }
    
    // Calculate organic matter contribution (decomposed leaves, twigs, etc.)
    // This adds dark patches that represent decomposing organic material
    private float CalculateOrganicMatter(int x, int y)
    {
        if (organicCenters.Length == 0 || organicDensity <= 0f) return 0f;
        
        float organicContribution = 0f;
        Vector2 currentPixel = new Vector2(x, y);
        
        // Check contribution from all organic matter locations
        foreach (Vector2 center in organicCenters)
        {
            float distance = Vector2.Distance(currentPixel, center);
            float normalizedDistance = distance / (organicSize * 5f);
            
            // Create soft, organic-looking shapes
            float organicIntensity = 1f - Mathf.Clamp01(normalizedDistance);
            organicIntensity = Mathf.Pow(organicIntensity, 0.8f); // Softer edges than pebbles
            
            // Add to total organic contribution
            organicContribution += organicIntensity * organicDensity;
        }
        
        return Mathf.Clamp01(organicContribution);
    }
    
    // Main function that combines all dirt layers into final pixel color
    private Color CalculateDirtPixelColor(int x, int y)
    {
        // Calculate all layer contributions
        float soilBase = CalculateBaseSoilPattern(x, y);
        float pebbleContribution = CalculatePebbleContribution(x, y);
        float fineParticles = CalculateFineParticles(x, y);
        float moistureLevel = CalculateMoistureLevel(x, y);
        float organicMatter = CalculateOrganicMatter(x, y);
        
        // Start with base soil color
        Color baseColor = Color.Lerp(darkDirtColor, lightDirtColor, soilBase);
        
        // Add fine particle variation
        float particleInfluence = soilBase + fineParticles;
        particleInfluence = Mathf.Clamp01(particleInfluence);
        baseColor = Color.Lerp(baseColor, lightDirtColor, fineParticles * 0.3f);
        
        // Apply moisture - wet areas become darker
        baseColor = Color.Lerp(baseColor, wetDirtColor, moistureLevel);
        
        // Add pebbles - they have their own color and override other layers
        if (pebbleContribution > 0.1f)
        {
            baseColor = Color.Lerp(baseColor, pebbleColor, pebbleContribution * 0.8f);
        }
        
        // Add organic matter - darkens the soil significantly
        if (organicMatter > 0.05f)
        {
            baseColor = Color.Lerp(baseColor, organicColor, organicMatter);
        }
        
        return baseColor;
    }
    
    // Public function to manually trigger texture generation
    [ContextMenu("Generate Dirt Texture")]
    public void ManualGenerateTexture()
    {
        Debug.Log("Manual dirt texture generation triggered");
        
        // Regenerate random positions
        pebbleCenters = GenerateRandomCenters(pebbleCount, textureWidth, textureHeight);
        organicCenters = GenerateRandomCenters(organicCount, textureWidth, textureHeight);
        
        // Generate new texture
        GenerateDirtTexture();
    }
    
    // Public function to get the generated texture (for other scripts)
    public Texture2D GetGeneratedTexture()
    {
        return generatedTexture;
    }
    
    // Save texture as PNG file (Editor only)
    [ContextMenu("Save Dirt Texture as PNG")]
    public void SaveTextureAsPNG()
    {
        #if UNITY_EDITOR
        if (generatedTexture != null)
        {
            byte[] pngData = generatedTexture.EncodeToPNG();
            string filename = $"PG_DirtTexture_{textureWidth}x{textureHeight}.png";
            string path = System.IO.Path.Combine(Application.dataPath, "Textures/ProcedurallyGenerated", filename);
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            // Write PNG data to file
            System.IO.File.WriteAllBytes(path, pngData);
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log($"Dirt texture saved as: {filename}");
        }
        else
        {
            Debug.LogWarning("No dirt texture to save! Generate texture first.");
        }
        #endif
    }
    
    // Clean up when object is destroyed
    void OnDestroy()
    {
        if (generatedTexture != null)
        {
            DestroyImmediate(generatedTexture);
        }
    }
}