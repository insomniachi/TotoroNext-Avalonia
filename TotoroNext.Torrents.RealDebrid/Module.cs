using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using TotoroNext.Torrents.RealDebrid.ViewModels;
using TotoroNext.Torrents.RealDebrid.Views;

namespace TotoroNext.Torrents.RealDebrid;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Name = "Real Debrid",
        Description = "Real Debrid service integration",
        Components = [ComponentTypes.Debrid],
        Id = new Guid("5511cd15-1d0b-4740-b526-087e0ef2de77"),
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("rd.jpg")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddHttpClient("RealDebrid", (sp, client) =>
        {
            var token = sp.GetRequiredService<IModuleSettings<Settings>>().Value.Token;
            client.BaseAddress = new Uri("https://api.real-debrid.com/rest/1.0/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
        services.AddKeyedTransient<IDebrid, RealDebridService>(Descriptor.Id);
    }
}

public class Settings : OverridableConfig
{
    public string Token { get; set; } = "";
}