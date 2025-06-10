using System;
using BeatSaberMarkupLanguage.Attributes;
using GameCoverColors.Configuration;
using GameCoverColors.Managers;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using Zenject;

namespace GameCoverColors.UI;

[UsedImplicitly]
internal class ModSettingsViewController : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("ModalToLetPeopleKnowThingHappened")]
    private ModalView? _modalToLetPeopleKnowThingHappened = null;
    [UIComponent("ExportConfirmationText")]
    private TextMeshProUGUI? _exportConfirmationText = null;
    // ReSharper restore FieldCanBeMadeReadOnly.Local
    
    public void Initialize()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("GameCoverColors",
            "GameCoverColors.UI.BSML.ModSettings.bsml", this);
    }

    public void Dispose()
    {
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
    }

    [UIAction("ExportColorScheme")]
    [UsedImplicitly]
    private void ExportColorScheme()
    {
        string exportMessage = SchemeExporter.ExportColorScheme();
        
        if (_modalToLetPeopleKnowThingHappened == null || _exportConfirmationText == null)
        {
            return;
        }

        _exportConfirmationText.text = exportMessage;
        _modalToLetPeopleKnowThingHappened.Show(true,true);
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