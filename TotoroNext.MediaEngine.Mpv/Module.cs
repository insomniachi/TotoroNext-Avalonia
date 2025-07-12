using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.MediaEngine.Mpv.ViewModels;
using TotoroNext.MediaEngine.Mpv.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Mpv;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("b8c3f0d2-1c5e-4f6a-9b7d-3f8e1c5f0d2a"),
        Name = "MPV Media Player",
        Description = "A module for integrating MPV media player into TotoroNext.",
        HeroImage = ResourceHelper.GetResource("mpv.jpeg"),
        Components = [ComponentTypes.MediaEngine],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IMediaPlayer, MpvMediaPlayer>(Descriptor.Id);
    }
}

public class Settings
{
    public string FileName { get; set; } = OperatingSystem.IsLinux() ? "/usr/bin/mpv" : "";
    public bool LaunchFullScreen { get; set; } = true;
}