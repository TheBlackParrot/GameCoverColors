using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace GameCoverColors.Utils;

// ported from https://github.com/mattnedrich/palette-maker/blob/master/javascript/histogram-palette-builder.js with some modifications
internal class HistogramPaletteBuilder
{
    private const int DimensionMax = 256;
    private readonly NativeArray<Color32> _pixels;

    // ReSharper disable once ConvertToPrimaryConstructor
    public HistogramPaletteBuilder(Texture2D texture)
    {
        _pixels = texture.GetPixelData<Color32>(0);
    }
    
    private static int GetKeyForPixel(Color pixel, int bucketsPerDimension) {
        int bucketSize = DimensionMax / bucketsPerDimension;
        
        int redBucket = (int)Math.Round(Mathf.Min(pixel.r * 255, 255) / bucketSize);
        int greenBucket = (int)Math.Round(Mathf.Min(pixel.g * 255, 255) / bucketSize);
        int blueBucket = (int)Math.Round(Mathf.Min(pixel.b * 255, 255) / bucketSize);

        return redBucket << 16 | greenBucket << 8 | blueBucket;
    }

    private static Color ComputeAverageColor(List<Color> pixels)
    {
        float totalRed = 0f;
        float totalGreen = 0f;
        float totalBlue = 0f;
        
        foreach (Color pixel in pixels)
        {
            totalRed += pixel.r;
            totalGreen += pixel.g;
            totalBlue += pixel.b;
        }
        
        return new Color(totalRed / pixels.Count, totalGreen / pixels.Count, totalBlue / pixels.Count);
    }
    
    public List<Color> BinPixels(int bucketsPerDimension)
    {
        Dictionary<int, List<Color>> bucketMap = new();
        
        foreach (Color pixel in _pixels)
        {
            int key = GetKeyForPixel(pixel, bucketsPerDimension);

            if (bucketMap.ContainsKey(key))
            {
                bucketMap[key].Add(pixel);
            }
            else
            {
                bucketMap[key] = [pixel];
            }
        }

        return bucketMap.Select(kv => kv.Value).OrderBy(bucket => bucket.Count).Reverse().Select(ComputeAverageColor).ToList();
    }
}