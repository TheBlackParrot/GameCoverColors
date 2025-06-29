using UnityEngine;

namespace GameCoverColors.Extensions;

public static class Color
{
    public static float GetYiq(this UnityEngine.Color color) => (color.r * 299) + (color.g * 587) + (color.b * 114);
    
    public static float GetHue(this UnityEngine.Color color)
    {
        float min = Mathf.Min(color.r, color.g, color.b);
        float max = Mathf.Max(color.r, color.g, color.b);
        float delta = max - min;
        
        float hue;
        if (delta == 0)
            hue = 0;
        else if (Mathf.Approximately(max, color.r))
            hue = (color.g - color.b) / delta % 6;
        else if (Mathf.Approximately(max, color.g))
            hue = 2 + (color.b - color.r) / delta;
        else
            hue = 4 + (color.r - color.g) / delta;

        return 60 * hue;
    }

    public static float GetSaturation(this UnityEngine.Color color)
    {
        float min = Mathf.Min(color.r, color.g, color.b);
        float max = Mathf.Max(color.r, color.g, color.b);
        float saturation = (min + max) / 2;

        if(Mathf.Approximately(min, max)) {
            saturation = 0;
        }
        else
        {
            float delta = max - min;
            saturation = saturation > 0.5 ? delta / (2 - max - min) : delta / (max + min);
        }
        
        return saturation;
    }

    public static float GetValue(this UnityEngine.Color color) => Mathf.Max(color.r, color.g, color.b);

    public static float GetVibrancy(this UnityEngine.Color color)
    {
        float min = Mathf.Min(color.r, color.g, color.b);
        float max = Mathf.Max(color.r, color.g, color.b, 0.001f);
            
        return (max + min) * (max - min) / max;
    }
}