using System;
using System.Collections.Generic;
using System.Linq;
#if !V1_29_1
using Unity.Collections;
#endif
using UnityEngine;

namespace GameCoverColors.Utils;

// ported from https://github.com/mattnedrich/palette-maker/blob/master/javascript/histogram-palette-builder.js with some modifications
internal class HistogramPaletteBuilder
{
    private const int DimensionMax = 256;
#if V1_29_1
    private readonly Color32[] _pixels;
#else
    private readonly NativeArray<Color32> _pixels;
#endif
    private const byte MaxByte = 255;

    // ReSharper disable once ConvertToPrimaryConstructor
    public HistogramPaletteBuilder(Texture2D texture)
    {
#if V1_29_1
        _pixels = texture.GetPixels32();
#else
        _pixels = texture.GetPixelData<Color32>(0);
#endif
    }
    
    private static int GetKeyForPixel(Color32 pixel, int bucketsPerDimension) {
        int bucketSize = DimensionMax / bucketsPerDimension;
        
        int redBucket = (int)Math.Round((double)Math.Min(pixel[0], MaxByte) / bucketSize);
        int greenBucket = (int)Math.Round((double)Math.Min(pixel[1], MaxByte) / bucketSize);
        int blueBucket = (int)Math.Round((double)Math.Min(pixel[2], MaxByte) / bucketSize);

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
        
        foreach (Color32 pixel in _pixels)
        {
            int key = GetKeyForPixel(pixel, bucketsPerDimension);

            if (bucketMap.ContainsKey(key))
            {
                bucketMap[key].Add(pixel);
            }
            else
            {
                bucketMap[key] = [(Color)pixel];
            }
        }

        return bucketMap.Select(kv => kv.Value).OrderBy(bucket => bucket.Count).Reverse().Select(ComputeAverageColor).ToList();
    }
}