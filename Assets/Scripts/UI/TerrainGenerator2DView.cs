using System;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGenerator2DView : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;

    private PerlinNoiseTerrainGenerator _terrainGenerator;
    private float[,] _heights;
    private bool _isTerrainFound = false;

    private void Start()
    {
        // Get the PerlinNoiseTerrainGenerator component from the scene
        _terrainGenerator = FindFirstObjectByType<PerlinNoiseTerrainGenerator>();
        if (_terrainGenerator == null)
        {
            Debug.LogError("PerlinNoiseTerrainGenerator component not found in the scene.");
        }else
        {
            _isTerrainFound = true;
            UpdateMinimap();
        }
    }

    private void Update()
    {
        if (_isTerrainFound && _terrainGenerator.IsRealTimeUpdateEnabled())
        {
            UpdateMinimap();
        }
    }

    private Texture2D ConvertToGrayscaleTexture(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = heightMap[x, y]; // normalized [0, 1]
                Color gray = new Color(value, value, value);
                texture.SetPixel(x, y, gray);
            }
        }

        texture.Apply();
        return texture;
    }
    
    private void UpdateMinimap()
    {
        _heights = _terrainGenerator.GetTerrainHeights();
        Texture2D minimapTexture = ConvertToGrayscaleTexture(_heights);
        minimapImage.texture = minimapTexture;
    }

}
