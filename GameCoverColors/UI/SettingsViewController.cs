using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using GameCoverColors.Configuration;
using GameCoverColors.Managers;
using HMUI;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace GameCoverColors.UI;

[UsedImplicitly]
internal class SettingsViewController : IInitializable, IDisposable, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    internal void NotifyPropertiesChanged()
    {
        PropertyInfo[] properties = typeof(SavedConfig).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).ToArray();
        // ReSharper disable once EntityNameCapturedOnly.Local
        foreach (PropertyInfo property in properties)
        {
            NotifyPropertyChanged(property.Name);   
        }
    }
    
    private static PluginConfig Config => PluginConfig.Instance;
    private static SavedConfig? SavedConfigInstance => SchemeManager.SavedConfigInstance;
    internal static SettingsViewController? Instance;
    
    private readonly GameplaySetup _gameplaySetup;
    private readonly GameplaySetupViewController _gameplaySetupViewController;
    private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
    
    [UIValue("blurChoices")] private readonly List<int> _blurChoices = [0, 7, 15, 23, 35, 63, 127, 135, 143];
    [UsedImplicitly] private string KernelSizeFormatter(int value) => _blurChoices[value].ToString();
    [UsedImplicitly] private string ContrastFormatter(int value) => $"{(value / 10):0}%";
    
    // ReSharper disable FieldCanBeMadeReadOnly.Local
    [UIComponent("saberACircle")] private ImageView? _saberACircle = null;
    [UIComponent("saberBCircle")] private ImageView? _saberBCircle = null;
    [UIComponent("lightACircle")] private ImageView? _lightACircle = null;
    [UIComponent("lightBCircle")] private ImageView? _lightBCircle = null;
    [UIComponent("boostACircle")] private ImageView? _boostACircle = null;
    [UIComponent("boostBCircle")] private ImageView? _boostBCircle = null;
    // ReSharper restore FieldCanBeMadeReadOnly.Local

    private SettingsViewController(GameplaySetup gameplaySetup,
        GameplaySetupViewController gameplaySetupViewController,
        StandardLevelDetailViewController standardLevelDetailViewController )
    {
        _gameplaySetup = gameplaySetup;
        _gameplaySetupViewController = gameplaySetupViewController;
        _standardLevelDetailViewController = standardLevelDetailViewController;
        
        Instance = this;
    }
    
    public void Initialize()
    {
        _gameplaySetup.AddTab("GameCoverColors", "GameCoverColors.UI.BSML.Settings.bsml", this);
        _gameplaySetupViewController.didActivateEvent += GameplaySetupViewController_DidActivateEvent;
    }

    public void Dispose()
    {
        _gameplaySetup.RemoveTab("GameCoverColors");
        _gameplaySetupViewController.didActivateEvent -= GameplaySetupViewController_DidActivateEvent;
    }

    private static void GameplaySetupViewController_DidActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
    }

    public void RefreshColors()
    {
        try
        {
            if (_saberACircle != null) _saberACircle.color = SchemeManager.Colors?.saberAColor ?? Color.clear;
            if (_saberBCircle != null) _saberBCircle.color = SchemeManager.Colors?.saberBColor ?? Color.clear;
            if (_lightACircle != null) _lightACircle.color = SchemeManager.Colors?.environmentColor0 ?? Color.clear;
            if (_lightBCircle != null) _lightBCircle.color = SchemeManager.Colors?.environmentColor1 ?? Color.clear;
            if (_boostACircle != null) _boostACircle.color = SchemeManager.Colors?.environmentColor0Boost ?? Color.clear;
            if (_boostBCircle != null) _boostBCircle.color = SchemeManager.Colors?.environmentColor1Boost ?? Color.clear;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    [UIAction("RefreshSettings")]
    [UsedImplicitly]
    private void RefreshSettings()
    {
        SchemeManager.BeatmapDidUpdateContent(_standardLevelDetailViewController,
            _standardLevelDetailViewController._contentIsOwnedAndReady
                ? StandardLevelDetailViewController.ContentType.OwnedAndReady
                : StandardLevelDetailViewController.ContentType.Loading, 
            true);
    }

    [UIAction("SaveSettings")]
    [UsedImplicitly]
    private async Task SaveSettings()
    {
        if (_standardLevelDetailViewController == null)
        {
            return;
        }

        try
        {
            SchemeManager.SaveOverrides(await SchemeManager.WaitForBeatmapLoaded(_standardLevelDetailViewController));
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    protected bool Enabled
    {
        get => Config.Enabled;
        set => Config.Enabled = value;
    }
    
    protected int KernelSize
    {
        get => SavedConfigInstance?.KernelSize ?? Config.KernelSize;
        set
        {
            if (SavedConfigInstance == null)
            {
                Config.KernelSize = value;
            }
            else
            {
                SavedConfigInstance.KernelSize = value;
            }
            
            NotifyPropertyChanged();
        }
    }

    protected int DownsampleFactor
    {
        get => SavedConfigInstance?.DownsampleFactor ?? Config.DownsampleFactor;
        set
        {
            if (SavedConfigInstance == null)
            {
                Config.DownsampleFactor = value;
            }
            else
            {
                SavedConfigInstance.DownsampleFactor = value;
            }
            
            NotifyPropertyChanged();
        }
    }
    
    protected int PaletteSize
    {
        get => SavedConfigInstance?.PaletteSize ?? Config.PaletteSize;
        set
        {
            if (SavedConfigInstance == null)
            {
                Config.PaletteSize = value;
            }
            else
            {
                SavedConfigInstance.PaletteSize = value;
            }
            
            NotifyPropertyChanged();
        }
    }
    
    protected int MinimumContrastDifference
    {
        get => SavedConfigInstance?.MinimumContrastDifference ?? Config.MinimumContrastDifference;
        set
        {
            if (SavedConfigInstance == null)
            {
                Config.MinimumContrastDifference = value;
            }
            else
            {
                SavedConfigInstance.MinimumContrastDifference = value;
            }

            NotifyPropertyChanged();
        }
    }
}