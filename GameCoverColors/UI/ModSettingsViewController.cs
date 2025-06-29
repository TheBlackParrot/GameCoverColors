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
#if PRE_V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu("GameCoverColors",
            "GameCoverColors.UI.BSML.ModSettings.bsml", this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("GameCoverColors",
            "GameCoverColors.UI.BSML.ModSettings.bsml", this);
#endif
    }

    public void Dispose()
    {
#if PRE_V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance?.RemoveSettingsMenu(this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
#endif
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

    [UIValue("DifferenceTypePreferenceChoices")]
    [UsedImplicitly]
    private object[] _differenceTypePreferenceChoices = ["Contrast", "Hue", "Saturation", "Value", "Vibrancy"];
    
    protected string DifferenceTypePreference
    {
        get => Config.DifferenceTypePreference;
        set
        {
            Config.DifferenceTypePreference = value;
            SettingsViewController.Instance?.NotifyPropertyChanged(nameof(SettingsViewController.MinNoteContrastDiffText));
        }
    }
}