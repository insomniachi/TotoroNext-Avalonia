using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.MediaEngine.Abstractions;
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
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings<SettingsViewModel, Settings>(this);
        services.AddKeyedTransient<IMediaPlayer, VlcMediaPlayer>(Descriptor.Id);

        if (OperatingSystem.IsWindows())
        {
            services.AddTransient<IInitializer, WindowsInitializer>();
        }
    }
}

public class Settings : OverridableConfig
{
    [DisplayName("Executable")]
    [SpecialEditorType(SpecialEditorType.FileBrowser)]
    [Icon(CommonIcons.FilePath)]
    public string FileName { get; set; } = OperatingSystem.IsLinux()
        ? "/usr/bin/vlc"
        : "";

    [DisplayName("Start in fullscreen")]
    [Icon(CommonIcons.Fullscreen)]
    public bool LaunchFullScreen { get; set; } = true;
}

internal class SettingsViewModel(IModuleSettings<Settings> data) : ModuleSettingsViewModel<Settings>(data);