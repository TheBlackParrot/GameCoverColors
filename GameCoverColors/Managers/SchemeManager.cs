using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if PRE_V1_39_1
using System.Threading;
#endif
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using GameCoverColors.ColorThief;
using GameCoverColors.Configuration;
using GameCoverColors.Extensions;
using GameCoverColors.UI;
#if PRE_V1_37_1
using IPA.Utilities;
#endif
using JetBrains.Annotations;
using Newtonsoft.Json;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;
using Color = UnityEngine.Color;

namespace GameCoverColors.Managers;

[UsedImplicitly]
internal class SchemeManager : IInitializable, IDisposable, IAffinity
{
    private static readonly string OverridesPath = Path.Combine(Plugin.UserDataPath, "Overrides");
    
    private static PluginConfig Config => PluginConfig.Instance;
    internal static SavedConfig? SavedConfigInstance;
    
    internal static ColorScheme? Colors;
    
    private static StandardLevelDetailViewController? _standardLevelDetailViewController;
    private static LevelPackDetailViewController? _levelPackDetailViewController;

    [Inject]
    public void Construct(StandardLevelDetailViewController standardLevelDetailViewController,
        LevelPackDetailViewController levelPackDetailViewController)
    {
        _standardLevelDetailViewController = standardLevelDetailViewController;
        _levelPackDetailViewController = levelPackDetailViewController;
    }

    public void Initialize()
    {
        if (!Directory.Exists(OverridesPath)) { Directory.CreateDirectory(OverridesPath); }
        
        if (_standardLevelDetailViewController == null)
        {
            Plugin.DebugMessage("_standardLevelDetailViewController is null");
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent += BeatmapDidUpdateContentWrapper;
    }
    
    public void Dispose()
    {
        if (_standardLevelDetailViewController == null)
        {
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent -= BeatmapDidUpdateContentWrapper;
    }
    
#if PRE_V1_37_1
    internal static async Task<IPreviewBeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
    {
        while (standardLevelDetailViewController.beatmapLevel == null)
        {
            await Task.Yield();
        }

        return standardLevelDetailViewController.beatmapLevel;
    }
#else
    internal static async Task<BeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
    {
        while (standardLevelDetailViewController._beatmapLevel == null)
        {
            await Task.Yield();
        }

        return standardLevelDetailViewController._beatmapLevel;
    }
#endif

    private class QuantizedColorVibrancyComparer : IComparer<QuantizedColor>
    {
        public int Compare(QuantizedColor x, QuantizedColor y)
        {
            float xMin = x.UnityColor.MinColorComponent();
            float xMax = Mathf.Max(x.UnityColor.maxColorComponent, 0.001f);
            float yMin = y.UnityColor.MinColorComponent();
            float yMax = Mathf.Max(y.UnityColor.maxColorComponent, 0.001f);
            
            float xVibrancy = ((xMax + xMin) * (xMax - xMin)) / xMax;
            float yVibrancy = ((yMax + yMin) * (yMax - yMin)) / yMax;
            
            switch (true)
            {
                case true when xVibrancy > yVibrancy:
                    return -1;
                case true when xVibrancy < yVibrancy:
                    return 1;
                default:
                    return 0;
            }
        }
    }
    
    private class QuantizedColorYiqComparer : IComparer<QuantizedColor>
    {
        public int Compare(QuantizedColor x, QuantizedColor y)
        {
            float yiqX = Config.PreferHueDifference ? x.UnityColor.GetHue() : x.UnityColor.GetYiq();
            float yiqY = Config.PreferHueDifference ? y.UnityColor.GetHue() : y.UnityColor.GetYiq();

            switch (true)
            {
                case true when yiqX > yiqY:
                    return -1;
                case true when yiqX < yiqY:
                    return 1;
                default:
                    return 0;
            }
        }
    }
    
    private const double Rad2Deg = Math.PI / 180;
    private static float GetYiqDifference(Color x, Color y) => Mathf.Abs(x.GetYiq() - y.GetYiq());
    private static double GetHueDifference(Color x, Color y) => Math.Sin(Math.Abs(x.GetHue() - y.GetHue()) / 2 * Rad2Deg);
    private static bool SwapColors(Color x, Color y) => Config.PreferHueDifference ? x.GetHue() < y.GetHue() : x.GetYiq() < y.GetYiq();

#if PRE_V1_37_1
    private static void LoadOverrides(IPreviewBeatmapLevel beatmapLevel)
#else
    private static void LoadOverrides(BeatmapLevel beatmapLevel)
#endif
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string overrideFilename = Path.Combine(OverridesPath, string.Join("", beatmapLevel.levelID.Where(c => !invalidCharacters.Contains(c))) + ".json");
        
        SavedConfigInstance = null;
        
        if (!File.Exists(overrideFilename))
        {
            SettingsViewController.Instance?.NotifyPropertiesChanged();
            return;
        }
        
        try
        {
            SavedConfigInstance = JsonConvert.DeserializeObject(File.ReadAllText(overrideFilename), typeof(SavedConfig)) as SavedConfig;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        SettingsViewController.Instance?.NotifyPropertiesChanged();
    }

#if PRE_V1_37_1
    internal static void SaveOverrides(IPreviewBeatmapLevel beatmapLevel)
#else
    internal static void SaveOverrides(BeatmapLevel beatmapLevel)
#endif
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string overrideFilename = Path.Combine(OverridesPath, string.Join("", beatmapLevel.levelID.Where(c => !invalidCharacters.Contains(c))) + ".json");
        
        Plugin.Log.Info($"Should save overrides to {overrideFilename}");
        File.WriteAllText(overrideFilename, JsonConvert.SerializeObject(SavedConfigInstance ?? new SavedConfig(Config), Formatting.Indented));
    }

    private static void BeatmapDidUpdateContentWrapper(StandardLevelDetailViewController viewController,
        StandardLevelDetailViewController.ContentType contentType)
    {
        _ = BeatmapDidUpdateContent(viewController, contentType);
    }

    // https://github.com/WentTheFox/BSDataPuller/blob/0e5349e59a39a28be26e4bb6027d72948fff6eac/Core/MapEvents.cs#L395
    internal static async Task BeatmapDidUpdateContent(StandardLevelDetailViewController viewController,
        StandardLevelDetailViewController.ContentType contentType,
        bool isManualRefresh = false)
    {
        if (_levelPackDetailViewController == null)
        {
            return;
        }
        
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady)
        {
            return;
        }
        
#if PRE_V1_37_1
        IPreviewBeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#else
        BeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#endif
        
        if (!isManualRefresh)
        {
            LoadOverrides(beatmapLevel);
        }
        
        int textureSize = SavedConfigInstance?.TextureSize ?? Config.TextureSize;

#if PRE_V1_37_1
        Sprite? coverSprite = await beatmapLevel.GetCoverImageAsync(CancellationToken.None);
#elif PRE_V1_39_1
        Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync(CancellationToken.None);
#else
        Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync();
#endif
        
        RenderTexture? activeRenderTexture = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(textureSize, textureSize, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        
        Texture2D readableTexture = new(textureSize, textureSize);
            
        try
        {
            RenderTexture.active = temporary;
            Graphics.Blit(coverSprite.texture, temporary);

            try
            {
                readableTexture.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
                readableTexture.Apply();
                
#if DEBUG
                await File.WriteAllBytesAsync("UserData/GameCoverColors/test-preblur.png", readableTexture.EncodeToPNG());
#endif

                if (SavedConfigInstance == null)
                {
                    if (Config.KernelSize > 0)
                    {
                        readableTexture = _levelPackDetailViewController._kawaseBlurRenderer.Blur(readableTexture,
                            (KawaseBlurRendererSO.KernelSize)Config.KernelSize - 1);
                    }
                }
                else
                {
                    if (SavedConfigInstance.KernelSize > 0)
                    {
                        readableTexture = _levelPackDetailViewController._kawaseBlurRenderer.Blur(readableTexture,
                            (KawaseBlurRendererSO.KernelSize)SavedConfigInstance.KernelSize - 1);
                    }
                }
            }
            finally
            {
                RenderTexture.active = activeRenderTexture;
            }
        }
        finally
        {
            RenderTexture.ReleaseTemporary(temporary);
        }
        
#if DEBUG
        await File.WriteAllBytesAsync("UserData/GameCoverColors/test-postblur.png", readableTexture.EncodeToPNG());
#endif
            
        List<QuantizedColor> colors = [];
        try
        {
            colors = ColorThief.ColorThief.GetPalette(readableTexture, (SavedConfigInstance?.PaletteSize ?? Config.PaletteSize) + 1);
            colors.Sort(new QuantizedColorVibrancyComparer());
            Plugin.DebugMessage($"Got {colors.Count} colors");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        Color saberAColor = colors[0].UnityColor;
        colors.RemoveAt(0);
        
        Color saberBColor = Color.magenta; // rider get off my ass

        float mostDifferentValue = 0;
        
        for (int i = 0; i < colors.Count; i++)
        {
            float diff = Config.PreferHueDifference
                ? (float)GetHueDifference(saberAColor, colors[i].UnityColor) * 1000
                : GetYiqDifference(saberAColor, colors[i].UnityColor);
            
            if (diff > (SavedConfigInstance?.MinimumContrastDifference ?? Config.MinimumContrastDifference))
            {
                if (!Config.PreferHueDifference || (Config.PreferHueDifference && colors[i].UnityColor.GetSaturation() < 0.1))
                {
                    saberBColor = colors[i].UnityColor;
                    colors.RemoveAt(i);
                    break;   
                }
            }
            
            // ReSharper disable once InvertIf
            if (diff > mostDifferentValue)
            {
                saberBColor = colors[i].UnityColor;
                mostDifferentValue = diff;
            }
        }

        if (SwapColors(saberAColor, saberBColor))
        {
            Color x = saberAColor;
            Color y = saberBColor;
            saberAColor = y;
            saberBColor = x;
        }
            
        Color boostAColor = colors[0].UnityColor;
        colors.RemoveAt(0);
        Color boostBColor = colors[0].UnityColor;
        colors.RemoveAt(0);
        if (SwapColors(boostAColor, boostBColor))
        {
            Color x = boostAColor;
            Color y = boostBColor;
            boostAColor = y;
            boostBColor = x;
        }
            
        colors.Sort(new QuantizedColorYiqComparer());
            
        Color lightAColor = colors[0].UnityColor;
        Color lightBColor = colors[1].UnityColor;
        if (SwapColors(lightAColor, lightBColor))
        {
            Color x = lightAColor;
            Color y = lightBColor;
            lightAColor = y;
            lightBColor = x;
        }
            
        Colors = new ColorScheme(
            "GameCoverColorsScheme",
            "GameCoverColorsScheme",
            true,
            "GameCoverColorsScheme",
            false,
#if V1_40_3
            true,
#endif
            Config.FlipNoteColors ? saberBColor : saberAColor, 
            Config.FlipNoteColors ? saberAColor : saberBColor,
#if V1_40_3
            true,
#endif
            Config.FlipLightSchemes
                ? (Config.FlipLightColors ? boostBColor : boostAColor)
                : (Config.FlipLightColors ? lightBColor : lightAColor),
            Config.FlipLightSchemes
                ? (Config.FlipLightColors ? boostAColor : boostBColor)
                : (Config.FlipLightColors ? lightAColor : lightBColor),
#if !V1_29_1
            Color.white,
#endif
            true,
            Config.FlipLightSchemes
                ? (Config.FlipBoostColors ? lightBColor : lightAColor)
                : (Config.FlipBoostColors ? boostBColor : boostAColor),
            Config.FlipLightSchemes
                ? (Config.FlipBoostColors ? lightAColor : lightBColor)
                : (Config.FlipBoostColors ? boostAColor : boostBColor),
#if !V1_29_1
            Color.white,
#endif
            Color.black
        );
        
        SettingsViewController.Instance?.SaveSettingsButton?.SetButtonText(SavedConfigInstance == null ? "Save" : "Overwrite");

        SettingsViewController.Instance?.RefreshColors();
    }

#if PRE_V1_37_1
    [AffinityPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.Init))]
#else
    [AffinityPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo))]
#endif
    [AffinityPostfix]
    // ReSharper disable once InconsistentNaming
    private void InitColorInfoPatch(StandardLevelScenesTransitionSetupDataSO __instance)
    {
        if (!Config.Enabled)
        {
            return;
        }
        
        __instance.usingOverrideColorScheme = true;
        __instance.colorScheme = Colors;
#if PRE_V1_37_1
        __instance.gameplayCoreSceneSetupData.SetField("colorScheme", Colors);
#endif
    }
}