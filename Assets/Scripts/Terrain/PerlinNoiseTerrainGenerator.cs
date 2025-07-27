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
    [Range(0f, 500f)]
    [SerializeField] private int yRange = 20;            // Depth of the terrain
    [Range(0f, 10f)]
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
        // Set the terrain offsets
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);
        
        // Get the Terrain component attached to this GameObject
        _terrain = GetComponent<Terrain>();
        
        // Change the terrain data of the Terrain component
        _terrain.terrainData = GenerateTerrain(_terrain.terrainData);
        
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
        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;
        
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
                
                // Track the actual min and max values
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                
                // Store the raw, unnormalized zRange for now
                heights[x, z] = noiseHeight;
            }
        }
        
        // Normalize the heights 
        for (int x = 0; x < xRange; x++)
        {
            for (int z = 0; z < zRange; z++)
            {
                // Normalize the stored zRange using the actual min/max
                float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heights[x, z]);
            
                // Apply redistribution to shape the terrain
                heights[x, z] = Mathf.Pow(normalizedHeight, redistributionExponent);
            }
        }
        
        return heights;
    }
}
