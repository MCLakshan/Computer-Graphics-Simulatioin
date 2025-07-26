using UnityEngine;
using UnityEngine.UI;

public class TextureGenerator_Sand : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private int grainCount = 1000; // Number of grains to generate
    
    [Header("Grain Parameters - Control sand grain appearance")]
    [Range(0.5f, 5f)]
    [SerializeField] private float grainSize = 1f;      // Size of individual grains
    [Range(0.1f, 3f)]
    [SerializeField] private float contrast = 1.5f;     // Sharpness of grain boundaries
    
    [Header("Surface Detail - Makes sand look rough and natural")]
    [Range(5f, 50f)]
    [SerializeField] private float detailScale = 20f;   // Frequency of surface roughness
    [Range(0f, 0.5f)]
    [SerializeField] private float detailStrength = 0.2f; // How much surface detail to add
    
    [Header("Sand Colors - Realistic sand appearance")]
    [SerializeField] private Color lightColor = new Color(0.9f, 0.85f, 0.7f, 1f);  // Light sand color
    [SerializeField] private Color darkColor = new Color(0.6f, 0.55f, 0.4f, 1f);   // Dark sand/shadow color
    
    [Header("Visualization")]
    [SerializeField] private RawImage uiRawImage;          // For UI display
    [SerializeField] private bool applyToMaterial = true;  // Apply to GameObject's material
    
    // Private variables - These store our working data
    private Vector2[] grainCenters;        // Positions of all grain centers
    private Texture2D generatedTexture;   // The final sand texture
    
    void Start()
    {
        Debug.Log("Sand Texture Generator Started!");
        
        // Generate grain positions
        grainCenters = GenerateGrainCenters(grainCount, textureWidth, textureHeight);
        
        // Create the texture for the first time
        CreateTexture();
        
        // Generate the sand texture
        GenerateSandTexture();
        
        Debug.Log($"Sand texture generated: {textureWidth}x{textureHeight} with {grainCount} grains");
    }
    
    // Create the texture object
    private void CreateTexture()
    {
        // Clean up existing texture
        if (generatedTexture != null)
        {
            DestroyImmediate(generatedTexture);
        }
        
        // Create new texture
        generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        generatedTexture.name = "Generated_Sand_Texture";
        generatedTexture.filterMode = FilterMode.Bilinear; // Smooth filtering
        generatedTexture.wrapMode = TextureWrapMode.Repeat; // Allow tiling
        
        Debug.Log("Texture object created");
    }
    
    // Main function that generates the complete sand texture
    private void GenerateSandTexture()
    {
        Debug.Log("Generating sand texture...");
        
        // Create array to store all pixel colors
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        // Generate each pixel's color
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                // Calculate the array index for this pixel
                int pixelIndex = y * textureWidth + x;
                
                // Calculate the color for this pixel using our algorithm
                pixels[pixelIndex] = CalculatePixelColor(x, y);
            }
        }
        
        // Apply all pixels to the texture at once (efficient)
        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply(); // Actually update the GPU texture
        
        // Display the texture
        DisplayTexture();
        
        Debug.Log("Sand texture generation complete!");
    }
    
    // Display the generated texture on UI and/or material
    private void DisplayTexture()
    {
        // Apply to RawImage UI component if assigned
        if (uiRawImage != null)
        {
            uiRawImage.texture = generatedTexture;
            Debug.Log("Texture applied to RawImage UI");
        }
        
        // Apply to GameObject's material if enabled
        if (applyToMaterial)
        {
            Renderer objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectRenderer.material.mainTexture = generatedTexture;
                Debug.Log("Texture applied to GameObject material");
            }
        }
    }
    
    // Step 1: Generate Random Grain Centers
    //  - Simulates natural grain distribution
    //  - No regular patterns (which would look artificial)
    //  - Covers entire texture area
    private Vector2[] GenerateGrainCenters(int grainCount, int width, int height)
    {
        Vector2[] centers = new Vector2[grainCount];
    
        for (int i = 0; i < grainCount; i++)
        {
            // Random position within texture bounds
            centers[i] = new Vector2(
                Random.Range(0f, width),   // X position
                Random.Range(0f, height)  // Y position
            );
        }
    
        return centers;
    }
    
    // Step 2: Calculate Voronoi Distance for Each Pixel
    //  - Brute force approach: Simple and reliable 
    //  - Distance function: Euclidean distance gives circular grain shapes
    //  - Minimum search: Each pixel belongs to the closest grain
    private float CalculateVoronoiDistance(int x, int y, Vector2[] grainCenters)
    {
        float minDistance = float.MaxValue;
        Vector2 currentPixel = new Vector2(x, y);
    
        // Check distance to every grain center
        foreach (Vector2 center in grainCenters)
        {
            float distance = Vector2.Distance(currentPixel, center);
        
            if (distance < minDistance)
            {
                minDistance = distance;  // This is our Voronoi value
            }
        }
    
        return minDistance;
    }
    
    // Step 3: Convert Distance to Grain Intensity
    // Why each step:
    // 1. Normalization: Raw distances vary with grain density, we need consistent scale
    // 2. Inversion: Distance is high at edges, but we want bright centers
    // 3. Clamping: Ensure values stay in [0,1] range
    // 4. Power function: Mathf.Pow(x, contrast) shapes the curve for grain sharpness
    private float ConvertDistanceToIntensity(float distance, float grainSize, float contrast)
    {
        // Normalize distance based on expected grain size
        float normalizedDistance = distance / (grainSize * 10f);
    
        // Invert: close to center = high value, far = low value
        float intensity = 1f - Mathf.Clamp01(normalizedDistance);
    
        // Apply contrast to sharpen grain boundaries
        // contrast < 1: Soft, gradual transitions
        // contrast > 1: Sharp, defined grain boundaries
        intensity = Mathf.Pow(intensity, contrast);
    
        return intensity;
    }
    
    // Step 4: Add Surface Detail with Perlin Noise
    // Why add this layer:
    // - Voronoi alone: Creates flat-topped grains (unrealistic)
    // - Perlin noise: Adds surface roughness and micro-variations
    // - High frequency: Creates fine detail that makes sand look natural
    private float AddSurfaceDetail(int x, int y, int width, int height, float scale, float strength)
    {
        // Convert pixel coordinates to noise coordinates
        float xCoord = (float)x / width * scale;
        float yCoord = (float)y / height * scale;
    
        // Generate Perlin noise
        float noiseValue = Mathf.PerlinNoise(xCoord, yCoord);
    
        // Center around 0 and scale
        // Perlin noise gives [0,1], we want [-strength/2, +strength/2]
        float detail = (noiseValue - 0.5f) * strength;
    
        return detail;
    }
    
    // Step 5: Combine Everything
    // Layer combination logic:
    // - Base layer: Voronoi provides grain structure
    // - Detail layer: Perlin adds surface texture
    // - Additive: base + detail creates natural variation
    // - Color mapping: Intensity controls light/shadow
    private Color CalculatePixelColor(int x, int y)
    {
        // Step 1: Get grain structure
        float voronoiDistance = CalculateVoronoiDistance(x, y, grainCenters);
        float grainIntensity = ConvertDistanceToIntensity(voronoiDistance, grainSize, contrast);
    
        // Step 2: Add surface detail
        float surfaceDetail = AddSurfaceDetail(x, y, textureWidth, textureHeight, detailScale, detailStrength);
    
        // Step 3: Combine layers
        float finalIntensity = grainIntensity + surfaceDetail;
        finalIntensity = Mathf.Clamp01(finalIntensity);  // Keep in valid range [0,1]
    
        // Step 4: Convert intensity to sand color
        // High intensity = light sand (grain centers)
        // Low intensity = dark sand (grain boundaries and shadows)
        Color sandColor = Color.Lerp(darkColor, lightColor, finalIntensity);
    
        return sandColor;
    }
    
    // Public function to manually trigger texture generation
    [ContextMenu("Generate Sand Texture")]
    public void ManualGenerateTexture()
    {
        Debug.Log("Manual texture generation triggered");
        grainCenters = GenerateGrainCenters(grainCount, textureWidth, textureHeight);
        GenerateSandTexture();
    }
    
    // Public function to get the generated texture (for other scripts)
    public Texture2D GetGeneratedTexture()
    {
        return generatedTexture;
    }
    
    // Save texture as PNG file (Editor only)
    [ContextMenu("Save Texture as PNG")]
    public void SaveTextureAsPNG()
    {
        #if UNITY_EDITOR
        if (generatedTexture != null)
        {
            byte[] pngData = generatedTexture.EncodeToPNG();
            string filename = $"PG_SandTexture_{textureWidth}x{textureHeight}.png";
            string path = System.IO.Path.Combine(Application.dataPath, "Textures/ProcedurallyGenerated", filename);
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            // Write PNG data to file
            System.IO.File.WriteAllBytes(path, pngData);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Sand texture saved as: {filename}");
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