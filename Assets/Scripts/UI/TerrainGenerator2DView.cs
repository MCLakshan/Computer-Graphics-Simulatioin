using System;
using UnityEngine;
using UnityEngine.UI;

public class TerrainGenerator2DView : MonoBehaviour
{
    [SerializeField]private PerlinNoiseTerrainGenerator _terrainGenerator;
    [SerializeField] private MinimapType minimapType;
    [SerializeField] private RawImage minimapImage;
    
    private float[,] _heights;
    
    [SerializeField] private TerrainType[] terrainTypes; // Array of terrain types for color mapping

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
                
                if (minimapType == MinimapType.Grayscale)
                {
                    Color gray = new Color(value, value, value);
                    texture.SetPixel(x, y, gray);
                }

                if (minimapType == MinimapType.ColorMapped)
                {
                    for(int regionIndex = 0; regionIndex < terrainTypes.Length; regionIndex++)
                    {
                        if (value <= terrainTypes[regionIndex].Height)
                        {
                            texture.SetPixel(x, y, terrainTypes[regionIndex].Color);
                            break; // Exit the loop once the correct region is found
                        }
                    }
                }
            }
        }

        texture.Apply();
        return texture;
    }
    
    public void UpdateMinimap()
    {
        _heights = _terrainGenerator.GetTerrainHeights();
        Texture2D minimapTexture = ConvertToGrayscaleTexture(_heights);
        minimapImage.texture = minimapTexture;
    }
}

[Serializable]
public class TerrainType
{
    public string Name;
    public Color Color;
    public float Height;
}

[Serializable]
public enum MinimapType
{
    Grayscale,
    ColorMapped
}
