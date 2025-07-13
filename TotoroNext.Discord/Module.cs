using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Discord.ViewModels;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Discord;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("b3329249-3e29-4625-8211-2934dade3c37"),
        Name = "Discord Rich Presense",
        HeroImage = ResourceHelper.GetResource("discord-logo.jpg"),
        Description = "Custom discord rich presense while watching",
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        // services.AddViewMap<SettingsPage, SettingsViewModel>();
        services.AddHostedService<RpcService>();
    }
}

public class Settings
{
    public bool IsEnabled { get; set; } = true;
}