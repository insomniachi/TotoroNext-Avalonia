using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Discord.ViewModels;
using TotoroNext.Discord.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Discord;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("b3329249-3e29-4625-8211-2934dade3c37"),
        Name = "Discord Rich Presence",
        HeroImage = ResourceHelper.GetResource("discord-logo.jpg"),
        Description = "Custom discord rich presence while watching",
        SettingViewModel = typeof(SettingsViewModel),
        Components = [ComponentTypes.Miscellaneous]
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddHostedService<RpcService>();
    }
}

public class Settings
{
    public bool IsEnabled { get; set; } = true;
}