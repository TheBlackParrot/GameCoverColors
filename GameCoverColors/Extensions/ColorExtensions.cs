using UnityEngine;

namespace GameCoverColors.Extensions;

public static class Color
{
    public static float MinColorComponent(this UnityEngine.Color color) => Mathf.Max(0.001f, Mathf.Min(Mathf.Min(color.r, color.g), color.b));
    public static float GetYiq(this UnityEngine.Color color) => (color.r * 299) + (color.g * 587) + (color.b * 114);
    
    // see HslColor.ToHsl()
    public static float GetHue(this UnityEngine.Color color)
    {
        float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
        float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
        float chroma = max - min;
        float h1;
        
        if (chroma == 0)
            h1 = 0;
        else if (Mathf.Approximately(max, color.r))
            h1 = (color.g - color.b) / chroma % 6;
        else if (Mathf.Approximately(max, color.g))
            h1 = 2 + (color.b - color.r) / chroma;
        else
            h1 = 4 + (color.r - color.g) / chroma;

        return 60 * h1;
    }

    public static float GetSaturation(this UnityEngine.Color color)
    {
        float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
        float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
        float chroma = max - min;
        
        float lightness = (max - min) / 2;
        return chroma == 0 ? 0 : chroma / (1 - Mathf.Abs(2 * lightness - 1));
    }

    public static float GetBrightness(this UnityEngine.Color color)
    {
        float max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
        float min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
        
        return (max - min) / 2;
    }

    public static float GetValue(this UnityEngine.Color color) => Mathf.Max(Mathf.Max(color.r, color.g), color.b);

    public static float GetVibrancy(this UnityEngine.Color color)
    {
        float min = color.MinColorComponent();
        float max = Mathf.Max(color.maxColorComponent, 0.001f);
            
        return (max + min) * (max - min) / max;
    }
}