using UnityEngine;
using UnityEngine.Events;

public class PerlinNoiseTerrainGenerator : MonoBehaviour
{
    // Script Description:
    [Header(
        "<b><size=13><color=#FFE066>Terrain Generator Script</color></size></b>\n" +
        "<i><color=#FFF2B2>This script generates a terrain zRange map using layered Perlin noise with octaves.</color></i>\n" +
        "<i><color=#FFF2B2> - If 'Real Time Update' is disabled, it generates the terrain once using the set values.</color></i>\n" +
        "<i><color=#FFF2B2> - Use 'Noise Scale' and 'Octaves' to control the surface detail.</color></i>"
    )]

    
    [Header("<b><color=#89CFF0><size=12>Terrain Settings</size></color></b>\n<i><color=#89CFF0>(Static - Not Editable in Runtime)</color></i>")]
    [SerializeField] private int xRange = 256;           // Width of the terrain
    [SerializeField] private int zRange = 256;          // Height of the terrain

    [Header("<b><color=#B0F2B6><size=12>Terrain Settings</size></color></b>\n<i><color=#B0F2B6>(Dynamic - Noise Settings)</color></i>")]
    [Range(0f, 2000f)]
    [SerializeField] private int yRange = 20;            // Depth of the terrain
    [Range(0f, 150f)]
    [SerializeField] private float noiseScale = 20f;    // Scale of the Perlin noise
    [Range(0f, 9999f)]
    [SerializeField] private float offsetX = 0f;        // Offset in the X direction for Perlin noise
    [Range(0f, 9999f)]
    [SerializeField] private float offsetZ = 0f;        // Offset in the Z direction for Perlin noise

    [Header("<b><color=#F7C59F><size=12>Terrain Settings</size></color></b>\n<i><color=#F7C59F>(Dynamic - Octaves Settings)</color></i>")]
    [Range(1, 8)]
    [SerializeField] private int octaves = 4;
    
    [Header("<b><color=#FFB677><size=12>Terrain Settings</size></color></b>\n<i><color=#FFB677>(Dynamic - Redistribution Settings)</color></i>")]
    [Range(0.1f, 5f)]
    [SerializeField] private float redistributionExponent = 1.5f;  // Controls valley/peak shaping of terrain

    [Header("<b><color=#E6E6FA><size=12>Island Settings</size></color></b>")]
    [SerializeField] private bool enableIslandMode = true;

    [Header("<b><color=#87CEEB><size=11>Beach Width Settings</size></color></b>")]
    [Range(0f, 1f)]
    [SerializeField] private float northBeachWidth = 0.3f;   // Top edge beach width
    [Range(0f, 1f)]
    [SerializeField] private float southBeachWidth = 0.3f;   // Bottom edge beach width
    [Range(0f, 1f)]
    [SerializeField] private float eastBeachWidth = 0.3f;    // Right edge beach width
    [Range(0f, 1f)]
    [SerializeField] private float westBeachWidth = 0.3f;    // Left edge beach width

    [Header("<b><color=#DDA0DD><size=11>Mountain Settings</size></color></b>")]
    [Range(0f, 3f)]
    [SerializeField] private float mountainCentralization = 1f; // How concentrated mountains are in the center

    [Header("<b><color=#FFD6E0><size=12>Control Settings</size></color></b>")]
    [SerializeField] private bool realTimeUpdate = true;        // Whether to update the terrain in real-time
    [Range(1, 20)]
    [SerializeField] private int updateRate = 1;                // Update rate for real-time updates

    [Header("<b><color=#FF9AA2><size=12>Events</size></color></b>")]
    public UnityEvent onTerrainGenerated; // Event to trigger when the terrain is generated
    
    private Terrain _terrain;
    private int _frameCount = 0;
    
    #region - GETTERS -
    
    // Get Heights of the terrain
    
    public float GetXRange() => xRange;
    
    public float[,] GetTerrainHeights() => _terrain.terrainData.GetHeights(0, 0, xRange, zRange);
    
    // Get is real-time update enabled
    public bool IsRealTimeUpdateEnabled() => realTimeUpdate;
    
    #endregion
    
    private void Awake()
    {
        // Get the Terrain component attached to this GameObject
        _terrain = GetComponent<Terrain>();
        
        // Change the terrain data of the Terrain component
        GenerateNewTerrain();
        
        // Invoke the event after terrain generation is complete
        onTerrainGenerated?.Invoke();
    }
    
    private void Update()
    {
        _frameCount++;
        
        // If real-time update is enabled, regenerate the terrain
        if (realTimeUpdate && _frameCount % updateRate == 0)
        {
            _terrain.terrainData = GenerateTerrain(_terrain.terrainData);
        }
        
        if (_frameCount > 100)
            _frameCount = 0;
    }

    // Make accessible method to inspector to regenerate terrain
    [ContextMenu("Generate New Terrain")]
    public void GenerateNewTerrain()
    {
        // Set the terrain offsets
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);
        
        _terrain.terrainData = GenerateTerrain(_terrain.terrainData);
    }

    // Generates the terrain data based terrain settings
    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        // Set the heightmap resolution = xRange + 1 because Unity requires the heightmap to have one more point than the xRange and length
        terrainData.heightmapResolution = xRange + 1; 
        
        // Set the size of the terrain (dimensions => xRange (x), length (z), zRange(y))
        terrainData.size = new Vector3(xRange, yRange, zRange);
        
        // Set the heightmap resolution
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }
    
    // Generates the heights for the terrain using Perlin noise (THE HEIGHT MAP)
    private float[,] GenerateHeights()
    {
        float[,] heights = new float[xRange, zRange];
        float minNoiseHeightPass1 = float.MaxValue;
        float maxNoiseHeightPass1 = float.MinValue;
        float minNoiseHeightPass2 = float.MaxValue;
        float maxNoiseHeightPass2 = float.MinValue;
        
        // Calculate terrain center
        float centerX = xRange * 0.5f;
        float centerZ = zRange * 0.5f;
        float maxDistance = Mathf.Min(xRange, zRange) * 0.5f; // Distance from center to edge
        
        // First pass: Generate base Perlin noise
        for (int x = 0; x < xRange; x++)
        {
            for (int z = 0; z < zRange; z++)
            {
                // Initialize variables for Perlin noise generation with octaves
                float noiseHeight = 0f;
                
                // Calculate the Coordinates for the Perlin noise
                float xCoordBase = (float)x / xRange * noiseScale + offsetX;
                float zCoordBase = (float)z / zRange * noiseScale + offsetZ;

                // Loop through the octaves to generate the Perlin noise
                for (int i = 0; i < octaves; i++)
                {
                    var octaveScale = Mathf.Pow(2, i);
                    
                    // Calculate the sample coordinates for the Perlin noise
                    float sampleX = xCoordBase * octaveScale;
                    float sampleZ = zCoordBase * octaveScale;

                    // Calculate the Perlin noise value and apply the zRange variation based on the octave
                    var perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                    var heightValue = (1.0f / octaveScale) * perlinValue;
                    noiseHeight += heightValue;
                }
                
                // Track the min and max values
                if (noiseHeight > maxNoiseHeightPass1) maxNoiseHeightPass1 = noiseHeight;
                if (noiseHeight < minNoiseHeightPass1) minNoiseHeightPass1 = noiseHeight;
                
                // Store the raw, unnormalized zRange for now
                heights[x, z] = noiseHeight;
            }
        }
        
        // Second pass: Normalize and apply island mask
        for (int x = 0; x < xRange; x++)
        {
            for (int z = 0; z < zRange; z++)
            {
                // Normalize the stored height using the actual min/max
                float normalizedHeight = Mathf.InverseLerp(minNoiseHeightPass1, maxNoiseHeightPass1, heights[x, z]);
        
                // Apply redistribution to shape the terrain
                normalizedHeight = Mathf.Pow(normalizedHeight, redistributionExponent);
            
                // ISLAND SHAPING: Calculate distance from center
                float distanceFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                float distanceRatio = distanceFromCenter / maxDistance;
            
                // Apply island mask with individual beach controls
                float islandMask = CalculateIslandMask(distanceRatio, x, z);
            
                // Track the min and max values
                if (normalizedHeight > maxNoiseHeightPass2) maxNoiseHeightPass2 = normalizedHeight;
                if (normalizedHeight < minNoiseHeightPass2) minNoiseHeightPass2 = normalizedHeight;
                
                // Apply island mask to terrain height
                heights[x, z] = normalizedHeight * islandMask;
            }
        }
        
        // Pass 3: Final normalization
        for (int x = 0; x < xRange; x++)
        {
            for (int z = 0; z < zRange; z++)
            {
                // Normalize the heights again using the new min/max
                heights[x, z] = Mathf.InverseLerp(minNoiseHeightPass2, maxNoiseHeightPass2, heights[x, z]);
                
                // Ensure heights are clamped between 0 and 1
                heights[x, z] = Mathf.Clamp01(heights[x, z]);
            }
        }
        
        // Print the Max and Min heights for debugging
        float debugMaxHeight = float.MinValue;
        float debugMinHeight = float.MaxValue;

        for (int x = 0; x < xRange; x++)
        {
            for (int z = 0; z < zRange; z++)
            {
                if (heights[x, z] > debugMaxHeight)
                {
                    debugMaxHeight = heights[x, z];
                }
                if (heights[x, z] < debugMinHeight)
                {
                    debugMinHeight = heights[x, z];
                }
            }
        }
        Debug.Log($"Final Normalized Heights - Max: {debugMaxHeight}, Min: {debugMinHeight}");
        Debug.Log($"Pass 1 Raw Noise - Max: {maxNoiseHeightPass1}, Min: {minNoiseHeightPass1}");
        Debug.Log($"Pass 2 After Redistribution - Max: {maxNoiseHeightPass2}, Min: {minNoiseHeightPass2}");
        
        return heights;
    }
    
    // Island mask function with individual beach width control
private float CalculateIslandMask(float distanceFromCenter, int x, int z)
{
    if (!enableIslandMode) return 1f; // No island effect

    // Calculate normalized position (0 to 1 for each axis)
    float normalizedX = (float)x / (xRange - 1); // 0 = west, 1 = east
    float normalizedZ = (float)z / (zRange - 1); // 0 = south, 1 = north

    // Calculate distance to each edge
    float distanceToWest = normalizedX;           // Distance from west edge
    float distanceToEast = 1f - normalizedX;     // Distance from east edge  
    float distanceToSouth = normalizedZ;         // Distance from south edge
    float distanceToNorth = 1f - normalizedZ;   // Distance from north edge

    // Calculate beach influence from each side
    float westBeachInfluence = Mathf.Max(0f, westBeachWidth - distanceToWest) / westBeachWidth;
    float eastBeachInfluence = Mathf.Max(0f, eastBeachWidth - distanceToEast) / eastBeachWidth;
    float southBeachInfluence = Mathf.Max(0f, southBeachWidth - distanceToSouth) / southBeachWidth;
    float northBeachInfluence = Mathf.Max(0f, northBeachWidth - distanceToNorth) / northBeachWidth;

    // If any beach influence is active, reduce height
    float maxBeachInfluence = Mathf.Max(
        westBeachInfluence,
        eastBeachInfluence,
        southBeachInfluence,
        northBeachInfluence
    );

    // Beach areas are completely flat
    if (maxBeachInfluence >= 1f)
    {
        return 0f; // Pure beach/water level
    }

    // Calculate distance from center for mountain shaping
    float centerX = xRange * 0.5f;
    float centerZ = zRange * 0.5f;
    float distanceFromCenterActual = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
    float maxDistanceFromCenter = Mathf.Min(xRange, zRange) * 0.5f;
    float normalizedCenterDistance = Mathf.Clamp01(distanceFromCenterActual / maxDistanceFromCenter);

    // Apply mountain centralization
    float mountainHeight = 1f - Mathf.Pow(normalizedCenterDistance, mountainCentralization);

    // Reduce mountain height based on beach influence
    float beachReduction = 1f - maxBeachInfluence;
    mountainHeight *= beachReduction;

    return Mathf.Max(0f, mountainHeight);
}
}

