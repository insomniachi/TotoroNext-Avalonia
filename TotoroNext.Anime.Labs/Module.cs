using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Labs;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("79cf9217-e30e-4451-97f4-404ddae85d06");
    
    public Descriptor Descriptor { get; } = new()
    {
        Name = "Labs",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        // HeroImage = ResourceHelper.GetResource("anikoto.png"),
        // SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        // services.AddHttpClient($"{Id}-api",client =>
        // {
        //     client.BaseAddress = new Uri(AnimeProvider.BaseUrl);
        //     client.DefaultRequestHeaders.Referrer = client.BaseAddress;
        //     client.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);
        //     client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        // });
        // services.AddViewMap<SettingsView, SettingsViewModel>();
    }
}

public class Settings : OverridableConfig
{
    
}