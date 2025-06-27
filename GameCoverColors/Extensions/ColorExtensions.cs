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
}