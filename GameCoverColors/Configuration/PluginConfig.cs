using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using JetBrains.Annotations;
// ReSharper disable RedundantDefaultMemberInitializer

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace GameCoverColors.Configuration;

[UsedImplicitly]
internal class PluginConfig
{
    public static PluginConfig Instance { get; set; } = null!;
    
    public virtual bool Enabled { get; set; } = true;
    
    public virtual int KernelSize { get; set; } = 1;
    public virtual int DownsampleFactor { get; set; } = 1;
    public virtual int PaletteSize { get; set; } = 16;
    public virtual int MinimumContrastDifference { get; set; } = 350;
    public virtual bool FlipNoteColors { get; set; } = false;
    public virtual bool FlipLightColors { get; set; } = false;
    public virtual bool FlipBoostColors { get; set; } = false;
    public virtual bool FlipLightSchemes { get; set; } = false;
}