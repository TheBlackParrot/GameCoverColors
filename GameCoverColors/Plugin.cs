using System.IO;
using GameCoverColors.Configuration;
using GameCoverColors.Installers;
using IPA;
using IPA.Config.Stores;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace GameCoverColors;

[Plugin(RuntimeOptions.SingleStartInit)]
[NoEnableDisable]
[UsedImplicitly]
internal class Plugin
{
    internal static IPALogger Log { get; private set; } = null!;
    
    internal static readonly string UserDataPath = Path.Combine(UnityGame.UserDataPath, "GameCoverColors");

    [Init]
    public Plugin(IPALogger ipaLogger, IPAConfig ipaConfig, Zenjector zenjector)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);
        
        PluginConfig c = ipaConfig.Generated<PluginConfig>();
        PluginConfig.Instance = c;
        
        zenjector.Install<MenuInstaller>(Location.Menu);
        
        if (!Directory.Exists(UserDataPath)) { Directory.CreateDirectory(UserDataPath); }
        
        Log.Info("Plugin loaded");
    }

    public static void DebugMessage(string message)
    {
        #if DEBUG
        Log.Info(message);
        #endif
    }
}