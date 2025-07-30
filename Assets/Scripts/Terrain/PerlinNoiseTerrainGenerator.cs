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
    [SerializeField] private IslandMode islandMode;
    [Range(0f, 1f)]
    [SerializeField] private float innerEdge = 0.3f;          // How much of the edge is beach (0.3 = 30%)
    [Range(2, 5)]
    [SerializeField] private int multiCenterCount = 3;        // Number of centers for multi-center islands (if applicable)
    [Range(0f, 1f)]
    [SerializeField] private float multiCenterInnerEdge = 0.2f; // Inner edge for multi-center islands (0.5 = 50% of the radius)
    [Range(0f, 1f)]
    [SerializeField] private float multiCenterRadius = 0.3f; // Radius of influence for each mountain center in multi-center mode
    
    [Header("<b><color=#FFD6E0><size=12>Control Settings</size></color></b>")]
    [SerializeField] private bool stopGenerationOnStart = true; // If true, terrain generation will stop on start

    [Header("<b><color=#FF9AA2><size=12>Events</size></color></b>")]
    public UnityEvent onTerrainGenerated; // Event to trigger when the terrain is generated
    
    private Terrain _terrain;
    private int _frameCount = 0;
    private Vector2[] mountainCenters;
    private float[] mountainWeights;
    
    #region - GETTERS -
    
    // Get Heights of the terrain
    
    public float GetXRange() => xRange;
    public float GetZRange() => zRange;
    public float GetYRange() => yRange;
    
    public float[,] GetTerrainHeights() => _terrain.terrainData.GetHeights(0, 0, xRange, zRange);
    
    #endregion
    
    private void Awake()
    {
        // Get the Terrain component attached to this GameObject
        _terrain = GetComponent<Terrain>();
        
        // Change the terrain data of the Terrain component
        GenerateNewTerrain();
    }
    

    // Make accessible method to inspector to regenerate terrain
    [ContextMenu("Generate New Terrain")]
    public void GenerateNewTerrain()
    {
        // If stop generation on start is enabled, skip terrain generation
        if (stopGenerationOnStart && _frameCount == 0)
        {
            Debug.Log("Terrain generation stopped on start.");
            // Invoke the terrain generated event
            onTerrainGenerated?.Invoke();
            return;
        }
        
        // Set the terrain offsets
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);
        
        // Generate mountain centers if using MultiCenter mode
        if (islandMode == IslandMode.MultiCenter)
        {
            GenerateRandomMountainCenters();
        }

        _terrain.terrainData = GenerateTerrain(_terrain.terrainData);
        
        // Invoke the terrain generated event
        onTerrainGenerated?.Invoke();
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
            
                // Apply island mask with individual beach controls
                float islandMask = CalculateIslandMask(x, z);
            
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
        return heights;
    }
    
    // Island mask function - smoothly fades only the outer edge of the island
    private float CalculateIslandMask(float x, float z)
    {
        // If RealCenter mode selected
        if (islandMode == IslandMode.RealCenter)
        {
            // Calculate terrain center
            float centerX = xRange * 0.5f;
            float centerZ = zRange * 0.5f;
            float maxDistance = Mathf.Min(xRange, zRange) * 0.5f; // Distance from center to edge
        
            // ISLAND SHAPING: Calculate distance from center
            float distanceFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
            float distanceRatio = distanceFromCenter / maxDistance;

            distanceRatio = Mathf.Clamp01(distanceRatio);

            // If inside the unaffected central area, use full height
            if (distanceRatio <= innerEdge)
                return 1f;

            // Apply smooth falloff from distance = 1 to inner edge
            float t = Mathf.InverseLerp(1f, innerEdge, distanceRatio); // 0 at edge, 1 at inner edge
            return Mathf.SmoothStep(0f, 1f, t);
        }
        
        // If MultiCenter mode selected
        if (islandMode == IslandMode.MultiCenter)
        {
            // Use the pre-generated mountain centers
            float maxMountainInfluence = 0f;
        
            for (int i = 0; i < mountainCenters.Length; i++)
            {
                float distanceFromMountain = Vector2.Distance(new Vector2(x, z), mountainCenters[i]);
            
                // Each mountain has its own influence radius
                float mountainInfluenceRadius = Mathf.Min(xRange, zRange) * multiCenterRadius; 
                float normalizedDistance = Mathf.Clamp01(distanceFromMountain / mountainInfluenceRadius);
            
                // Calculate mountain influence (1 = full height, 0 = no influence)
                float mountainInfluence = 0f;
                if (normalizedDistance <= multiCenterInnerEdge)
                {
                    // Full influence if within the inner edge
                    mountainInfluence = mountainWeights[i]; 
                }
                else
                {
                    // Smooth falloff from edge to inner edge
                    float t = Mathf.InverseLerp(1f, multiCenterInnerEdge, normalizedDistance);
                    mountainInfluence = Mathf.SmoothStep(0f, mountainWeights[i], t);
                }
            
                // Take the maximum influence from any mountain
                maxMountainInfluence = Mathf.Max(maxMountainInfluence, mountainInfluence);
            }
            return maxMountainInfluence;
        }

        // Default case
        return 1;
    }
    
    private void GenerateRandomMountainCenters()
    {
        float centerX = xRange * 0.5f;
        float centerZ = zRange * 0.5f;
        float maxDistance = Mathf.Min(xRange, zRange) * 0.5f;
        float maxRange = maxDistance * multiCenterInnerEdge;
    
        mountainCenters = new Vector2[multiCenterCount];
        mountainWeights = new float[multiCenterCount];
    
        // Generate centers ONCE for the entire terrain
        for (int i = 0; i < multiCenterCount; i++)
        {
            float randomAngle = Random.Range(0f, 2f * Mathf.PI);
            float randomRadius = Random.Range(0f, maxRange) * Mathf.Sqrt(Random.Range(0f, 1f));
        
            mountainCenters[i] = new Vector2(
                centerX + randomRadius * Mathf.Cos(randomAngle),
                centerZ + randomRadius * Mathf.Sin(randomAngle)
            );
        
            // Optional: Give each mountain different strength
            mountainWeights[i] = Random.Range(0.7f, 1f);
        }
    }
}

[System.Serializable]
public enum IslandMode
{
    None,
    RealCenter,
    MultiCenter,
}


