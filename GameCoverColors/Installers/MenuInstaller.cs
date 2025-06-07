using GameCoverColors.Managers;
using GameCoverColors.UI;
using JetBrains.Annotations;
using Zenject;

namespace GameCoverColors.Installers;

[UsedImplicitly]
internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<ModSettingsViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<SettingsViewController>().AsSingle();
        
        Container.BindInterfacesTo<SchemeManager>().AsSingle();
    }
}