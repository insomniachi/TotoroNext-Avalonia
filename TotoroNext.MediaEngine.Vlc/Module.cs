using Microsoft.Extensions.DependencyInjection;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.MediaEngine.Vlc.ViewModels;
using TotoroNext.MediaEngine.Vlc.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Vlc;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("a5c4c1d1-4669-4423-bb77-d5285776b5c9"),
        Name = "VLC Media Player",
        Description = "A module for integrating VLC media player into TotoroNext.",
        HeroImage = ResourceHelper.GetResource("vlc.jpeg"),
        Components = [ComponentTypes.MediaEngine],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IMediaPlayer, VlcMediaPlayer>(Descriptor.Id);
    }
}

public class Settings
{
    public string FileName { get; set; } = OperatingSystem.IsLinux()
        ? "/usr/bin/vlc"
        : "";

    public bool LaunchFullScreen { get; set; } = true;
}