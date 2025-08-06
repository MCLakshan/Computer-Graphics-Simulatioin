using UnityEngine;

public class HELPER_CombinedWaterMap : MonoBehaviour
{
    [SerializeField] private int gridSize = 256;
    [SerializeField] private HELPER_TerrainGridFilter helperTerrainGridFilter;
    [SerializeField] private InnerTerrainWaterDitection[] innerTerrainWaterDitectionComponents;
    
    private int[,] waterMap;
    private bool isGeneratedCombinedWaterMap = false;

    private void Start()
    {
        isGeneratedCombinedWaterMap = false;
    }
    
    [ContextMenu("Create Combined Water Map")]
    private void CreateCombinedWaterMap()
    {
        waterMap = new int[gridSize, gridSize];
        
        // Initialize the water map with zeros
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                waterMap[x, y] = 0;
            }
        }
        

        foreach (var waterDetector in innerTerrainWaterDitectionComponents)
        {
            if(gridSize != waterDetector.GetGridSize())
            {
                Debug.LogError("Grid size mismatch between combined water map and inner terrain water detection components.");
                return;
            }
            
            int[,] detectorWaterMap = waterDetector.GetWaterMapAndClearMemory();
            
            // Update the combined water map
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (detectorWaterMap[x, y] == 1)
                    {
                        waterMap[x, y] = 1; // Mark as water
                    }
                }
            }
            
        }
        isGeneratedCombinedWaterMap = true;
        Debug.Log("Combined water map created successfully.");
    }
    
    [ContextMenu("Save Combined Water Map as Texture")]
    private void SaveWaterMapAsTexture()
    {
        Texture2D texture = new Texture2D(gridSize, gridSize);
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Color color = waterMap[x, y] == 1 ? Color.blue : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        
        // Save the texture to a file
        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/Scripts/Terrain/CombinedWaterMap.png";
        System.IO.File.WriteAllBytes(path, bytes);
        
        Debug.Log("Combined water map saved to: " + path);
    }
    
    // check if a given point is in water
    public bool IsPointInWater(float x, float y)
    {
        if (helperTerrainGridFilter == null)
        {
            Debug.LogError("HelperTerrainGridFilter reference is missing!");
            return false;
        }
        
        // Ensure the combined water map is generated
        if (!isGeneratedCombinedWaterMap)
        {
            CreateCombinedWaterMap();
        }
        
        Vector2Int gridCoords = helperTerrainGridFilter.GetGridForPoint(x, y, gridSize);
        
        if (gridCoords.x < 0 || gridCoords.y < 0 || gridCoords.x >= gridSize || gridCoords.y >= gridSize)
        {
            Debug.LogError($"Point [{x},{y}] is out of bounds for the water map.");
            return false;
        }
        
        return waterMap[gridCoords.x, gridCoords.y] == 1;
    }
}
