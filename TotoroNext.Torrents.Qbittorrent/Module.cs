using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using TotoroNext.Torrents.Qbittorrent.ViewModels;
using TotoroNext.Torrents.Qbittorrent.Views;

namespace TotoroNext.Torrents.Qbittorrent;

public class Module : IModule<Settings>
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<ITorrentClient, Client>(Descriptor.Id);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
    }

    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("305acd2c-0b08-4bf4-ad2d-de5e24ecc43f"),
        Name = "Qbittorent Client",
        HeroImage = ResourceHelper.GetResource("qbittorrent.jpg"),
        SettingViewModel = typeof(SettingsViewModel),
        Components = [ComponentTypes.TorrentClient]
    };
}

public class Settings : OverridableConfig
{
    public string Url { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}