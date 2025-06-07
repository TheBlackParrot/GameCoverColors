using System;
using GameCoverColors.Configuration;
using JetBrains.Annotations;
using Zenject;

namespace GameCoverColors.UI;

[UsedImplicitly]
internal class ModSettingsViewController : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    public void Initialize()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("GameCoverColors",
            "GameCoverColors.UI.BSML.ModSettings.bsml", this);
    }

    public void Dispose()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
    }

    protected bool FlipNoteColors
    {
        get => Config.FlipNoteColors;
        set => Config.FlipNoteColors = value;
    }
    protected bool FlipLightColors
    {
        get => Config.FlipLightColors;
        set => Config.FlipLightColors = value;
    }
    protected bool FlipBoostColors
    {
        get => Config.FlipBoostColors;
        set => Config.FlipBoostColors = value;
    }
    protected bool FlipLightSchemes
    {
        get => Config.FlipLightSchemes;
        set => Config.FlipLightSchemes = value;
    }
}