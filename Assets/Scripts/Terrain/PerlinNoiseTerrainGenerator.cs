using UnityEngine;

public class PerlinNoiseTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings (Static - Not Editable in Runtime)")]
    [SerializeField] private int width = 256;           // Width of the terrain
    [SerializeField] private int height = 256;          // Height of the terrain
    
    [Header("Terrain Settings (Dynamic - Editable in Runtime)")]
    [Range(0f, 100f)]
    [SerializeField] private int depth = 20;            // Depth of the terrain
    [Range(0f, 10f)]
    [SerializeField] private float noiseScale = 20f;    // Scale of the Perlin noise
    [SerializeField] private float offsetX = 0f;        // Offset in the X direction for Perlin noise
    [SerializeField] private float offsetZ = 0f;        // Offset in the Z direction for Perlin noise
    [SerializeField] private float exponent = 1f;         // Exponent to control the height variation
    
    [Header("Control Settings")]
    [SerializeField] bool realTimeUpdate = true;        // Whether to update the terrain in real-time
    [Range(1, 20)]
    [SerializeField] private int updateRate = 1;        // Update rate for real-time updates
    

    private int _frameCount = 0;                        // Frame counter for controlling update rate
    
    
    private void Start()
    {
        // Set the terrain offsets
        offsetX = Random.Range(0f, 9999f);
        offsetZ = Random.Range(0f, 9999f);
        
        Terrain terrain = GetComponent<Terrain>();
        // Change the terrain data of the Terrain component
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }
    
    private void Update()
    {
        _frameCount++;
        
        // If real-time update is enabled, regenerate the terrain
        if (realTimeUpdate && _frameCount % updateRate == 0)
        {
            Terrain terrain = GetComponent<Terrain>();
            terrain.terrainData = GenerateTerrain(terrain.terrainData);
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
                // Calculate the Perlin noise value at the given coordinates
                float xCoord = (float)x / width * noiseScale + offsetX;
                float zCoord = (float)z / height * noiseScale + offsetZ;
                var e = Mathf.PerlinNoise(xCoord, zCoord);
                heights[x, z] = Mathf.Pow(e, exponent);
            }
        }
        return heights;
    }
}
