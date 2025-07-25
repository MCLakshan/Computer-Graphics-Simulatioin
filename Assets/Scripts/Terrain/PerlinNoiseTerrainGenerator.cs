using UnityEngine;

public class PerlinNoiseTerrainGenerator : MonoBehaviour
{
    // Script Description:
    [Header(
        "<b><size=13><color=#FFE066>Terrain Generator Script</color></size></b>\n" +
        "<i><color=#FFF2B2>This script generates a terrain height map using layered Perlin noise with octaves.</color></i>\n" +
        "<i><color=#FFF2B2> - If 'Real Time Update' is disabled, it generates the terrain once using the set values.</color></i>\n" +
        "<i><color=#FFF2B2> - Use 'Noise Scale' and 'Octaves' to control the surface detail.</color></i>"
    )]

    
    [Header("<b><color=#89CFF0><size=12>Terrain Settings</size></color></b>\n<i><color=#89CFF0>(Static - Not Editable in Runtime)</color></i>")]
    [SerializeField] private int width = 256;           // Width of the terrain
    [SerializeField] private int height = 256;          // Height of the terrain

    [Header("<b><color=#B0F2B6><size=12>Terrain Settings</size></color></b>\n<i><color=#B0F2B6>(Dynamic - Noise Settings)</color></i>")]
    [Range(0f, 500f)]
    [SerializeField] private int depth = 20;            // Depth of the terrain
    [Range(0f, 10f)]
    [SerializeField] private float noiseScale = 20f;    // Scale of the Perlin noise
    [SerializeField] private float offsetX = 0f;        // Offset in the X direction for Perlin noise
    [SerializeField] private float offsetZ = 0f;        // Offset in the Z direction for Perlin noise

    [Header("<b><color=#F7C59F><size=12>Terrain Settings</size></color></b>\n<i><color=#F7C59F>(Dynamic - Octaves Settings)</color></i>")]
    [Range(1, 8)]
    [SerializeField] private int octaves = 4;

    [Header("<b><color=#FFD6E0><size=12>Control Settings</size></color></b>")]
    [SerializeField] private bool realTimeUpdate = true;        // Whether to update the terrain in real-time
    [Range(1, 20)]
    [SerializeField] private int updateRate = 1;                // Update rate for real-time updates

    private Terrain _terrain;
    private int _frameCount = 0;

    
    
    private void Start()
    {
        // Set the terrain offsets
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);
        
        // Get the Terrain component attached to this GameObject
        _terrain = GetComponent<Terrain>();
        
        
        // Change the terrain data of the Terrain component
        _terrain.terrainData = GenerateTerrain(_terrain.terrainData);
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
        // Set the heightmap resolution = width + 1 because Unity requires the heightmap to have one more point than the width and length
        terrainData.heightmapResolution = width + 1; 
        
        // Set the size of the terrain (dimensions => width (x), length (z), height(y))
        terrainData.size = new Vector3(width, depth, height);
        
        // Set the heightmap resolution
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }
    
    // Generates the heights for the terrain using Perlin noise (THE HEIGHT MAP)
    private float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Initialize variables for Perlin noise generation with octaves
                float noiseHeight = 0f;
                float possibleMinHeight = 0f;   // Must be always 0
                float possibleMaxHeight = 0f;   // Must be increased with each octave

                // Calculate the Coordinates for the Perlin noise
                float xCoordBase = (float)x / width * noiseScale + offsetX;
                float zCoordBase = (float)z / height * noiseScale + offsetZ;

                // Loop through the octaves to generate the Perlin noise
                for (int i = 0; i < octaves; i++)
                {
                    var octaveScale = Mathf.Pow(2, i);
                    
                    // Calculate the possible height variation for this octave
                    possibleMaxHeight += 1.0f / octaveScale; // Each octave contributes a height of 1 / octaveScale
                    
                    // Calculate the sample coordinates for the Perlin noise
                    float sampleX = xCoordBase * octaveScale;
                    float sampleZ = zCoordBase * octaveScale;

                    // Calculate the Perlin noise value and apply the height variation based on the octave
                    var perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                    var heightValue = (1.0f / octaveScale) * perlinValue;
                    noiseHeight += heightValue;
                }
                
                // Normalize the noise height to the range [0, 1]
                noiseHeight = Mathf.InverseLerp(possibleMinHeight, possibleMaxHeight, noiseHeight);
                heights[x, z] = noiseHeight;
            }
        }
        return heights;
    }
}
