using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameCoverColors.ColorThief;
using GameCoverColors.Configuration;
using GameCoverColors.Extensions;
using GameCoverColors.UI;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;
using Color = UnityEngine.Color;

namespace GameCoverColors.Managers;

[UsedImplicitly]
internal class SchemeManager : IInitializable, IDisposable, IAffinity
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    internal static ColorScheme? Colors;
    
    private StandardLevelDetailViewController? _standardLevelDetailViewController;
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
        if (_standardLevelDetailViewController == null)
        {
            Plugin.DebugMessage("_standardLevelDetailViewController is null");
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent += BeatmapDidUpdateContent;
    }
    
    public void Dispose()
    {
        if (_standardLevelDetailViewController == null)
        {
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent -= BeatmapDidUpdateContent;
    }
    
#if PRE_V1_37_1
    private static async Task<IPreviewBeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
    {
        while (standardLevelDetailViewController.beatmapLevel == null)
        {
            await Task.Yield();
        }

        return standardLevelDetailViewController.beatmapLevel;
    }
#else
    private static async Task<BeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
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
            float yiqX = GetYiq(x.UnityColor);
            float yiqY = GetYiq(y.UnityColor);

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

    private static float GetYiq(Color color) => (color.r * 299) + (color.g * 587) + (color.b * 114);
    private static float GetYiqDifference(Color x, Color y) => Mathf.Abs(GetYiq(x) - GetYiq(y));
    private static bool SwapColors(Color x, Color y) => GetYiq(x) < GetYiq(y);

    // https://github.com/WentTheFox/BSDataPuller/blob/0e5349e59a39a28be26e4bb6027d72948fff6eac/Core/MapEvents.cs#L395
    internal static void BeatmapDidUpdateContent(StandardLevelDetailViewController viewController,
        StandardLevelDetailViewController.ContentType contentType)
    {
        if (!Config.Enabled)
        {
            return;
        }
        
        if (_levelPackDetailViewController == null)
        {
            return;
        }
        
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady)
        {
            return;
        }
        
        UnityMainThreadTaskScheduler.Factory.StartNew<Task>(async () =>
        {
#if PRE_V1_37_1
            IPreviewBeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#else
            BeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#endif
            
#if PRE_V1_37_1
            Sprite? coverSprite = await beatmapLevel.GetCoverImageAsync(CancellationToken.None);
#elif PRE_V1_39_1
            Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync(CancellationToken.None);
#else
            Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync();
#endif
            
            RenderTexture? activeRenderTexture = RenderTexture.active;
            Texture2D? coverTexture = coverSprite.texture;
            RenderTexture? temporary = RenderTexture.GetTemporary(coverTexture.width, coverTexture.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Texture2D? readableTexture;
            
            try
            {
                Graphics.Blit(coverTexture, temporary);
                RenderTexture.active = temporary;

                try
                {
                    Rect textureRect = coverSprite.textureRect;
                    readableTexture = new Texture2D((int)textureRect.width, (int)textureRect.height);
                    
                    readableTexture.ReadPixels(
                        textureRect,
                        0,
                        0
                    );
                    readableTexture.Apply();

                    if (Config.KernelSize > 0)
                    {
                        readableTexture = _levelPackDetailViewController._kawaseBlurRenderer.Blur(readableTexture,
                            (KawaseBlurRendererSO.KernelSize)Config.KernelSize - 1, Config.DownsampleFactor);
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
            
            List<QuantizedColor> colors = [];
            try
            {
                colors = ColorThief.ColorThief.GetPalette(readableTexture, Config.PaletteSize + 1, 1);
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
            for (int i = 0; i < colors.Count; i++)
            {
                saberBColor = colors[i].UnityColor;

                // ReSharper disable once InvertIf
                if (GetYiqDifference(saberAColor, saberBColor) > Config.MinimumContrastDifference)
                {
                    colors.RemoveAt(i);
                    break;
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
                Color.white,
                true,
                Config.FlipLightSchemes
                    ? (Config.FlipBoostColors ? lightBColor : lightAColor)
                    : (Config.FlipBoostColors ? boostBColor : boostAColor),
                Config.FlipLightSchemes
                    ? (Config.FlipBoostColors ? lightAColor : lightBColor)
                    : (Config.FlipBoostColors ? boostAColor : boostBColor),
                Color.white,
                Color.black
            );

            SettingsViewController.Instance?.RefreshColors();
        });
    }

    [AffinityPatch(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.InitColorInfo))]
    [AffinityPostfix]
    // ReSharper disable once InconsistentNaming
    private void InitColorInfoPatch(StandardLevelScenesTransitionSetupDataSO __instance)
    {
        Plugin.DebugMessage("InitColorInfo called");
        if (!Config.Enabled)
        {
            return;
        }
        
        __instance.usingOverrideColorScheme = true;
        __instance.colorScheme = Colors;
        Plugin.DebugMessage("Got here");
    }
}