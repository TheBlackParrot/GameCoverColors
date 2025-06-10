using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GameCoverColors.Managers;

[JsonObject(MemberSerialization.OptIn)]
internal readonly struct ColorConverted
{
    // ReSharper disable InconsistentNaming
    private float r { get; } = 1f;
    private float g { get; } = 1f;
    private float b { get; } = 1f;
    
    private int intR => Mathf.RoundToInt(r * 255);
    private int intG => Mathf.RoundToInt(g * 255);
    private int intB => Mathf.RoundToInt(b * 255);
    
    [JsonProperty] internal readonly Dictionary<string, float> asFloat;
    [JsonProperty] internal readonly Dictionary<string, int> asInteger;
    [JsonProperty] internal string asHex => $"#{ColorUtility.ToHtmlStringRGB(this)}";
    // ReSharper restore InconsistentNaming

    private ColorConverted(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;

        asFloat = new Dictionary<string, float>
        {
            { "r", r },
            { "g", g },
            { "b", b }
        };

        asInteger = new Dictionary<string, int>
        {
            { "r", intR },
            { "g", intG },
            { "b", intB }
        };
    }
    
    public static implicit operator Color(ColorConverted color)
    {
        Color output = new(color.r, color.g, color.b, 1f);
        return output;
    }

    public static implicit operator ColorConverted(Color color)
    {
        ColorConverted output = new(color.r, color.g, color.b);
        return output;
    }
}

internal abstract class SchemeExporter
{
    private static readonly string ExportsPath = Path.Combine(Plugin.UserDataPath, "Exports");

    public static string ExportColorScheme()
    {
        if (!Directory.Exists(ExportsPath)) { Directory.CreateDirectory(ExportsPath); }

        if (SchemeManager.Colors == null)
        {
            return "No color scheme has been generated";
        }

        Dictionary<string, ColorConverted> mapInfoFormat = new()
        {
            { "_saberAColor", SchemeManager.Colors._saberAColor },
            { "_saberBColor", SchemeManager.Colors._saberBColor },
            { "_environmentColor0", SchemeManager.Colors._environmentColor0 },
            { "_environmentColor1", SchemeManager.Colors._environmentColor1 },
            { "_environmentColor0Boost", SchemeManager.Colors._environmentColor0Boost },
            { "_environmentColor1Boost", SchemeManager.Colors._environmentColor1Boost }
        };

        string filename = $"{DateTime.Now:yyyyMMdd_HHmmss}.json";
        File.WriteAllText(Path.Combine(ExportsPath, filename), 
            JsonConvert.SerializeObject(mapInfoFormat, Formatting.Indented));

        return $"Exported color scheme to {filename}";
    }
}