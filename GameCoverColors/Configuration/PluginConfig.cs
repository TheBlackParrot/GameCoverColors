using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable RedundantDefaultMemberInitializer

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace GameCoverColors.Configuration;

[UsedImplicitly]
internal class PluginConfig
{
    public static PluginConfig Instance { get; set; } = null!;
    
    public virtual bool Enabled { get; set; } = true;
    
    public virtual int TextureSize { get; set; } = 64;
    public virtual int KernelSize { get; set; } = 1;
    public virtual int PaletteSize { get; set; } = 16;
    public virtual int MinimumContrastDifference { get; set; } = 350;
    public virtual bool FlipNoteColors { get; set; } = false;
    public virtual bool FlipLightColors { get; set; } = false;
    public virtual bool FlipBoostColors { get; set; } = false;
    public virtual bool FlipLightSchemes { get; set; } = false;
}

[JsonObject(MemberSerialization.OptIn)]
internal sealed class SavedConfig
{
    private static PluginConfig Config => PluginConfig.Instance;
    [JsonProperty] public int TextureSize { get; set; } = Config.TextureSize;
    [JsonProperty] public int KernelSize { get; set; } = Config.KernelSize;
    [JsonProperty] public int PaletteSize { get; set; } = Config.PaletteSize;
    [JsonProperty] public int MinimumContrastDifference { get; set; } = Config.MinimumContrastDifference;

    internal SavedConfig(PluginConfig config)
    {
        TextureSize = config.TextureSize;
        KernelSize = config.KernelSize;
        PaletteSize = config.PaletteSize;
        MinimumContrastDifference = config.MinimumContrastDifference;
    }

    internal SavedConfig() { }
}