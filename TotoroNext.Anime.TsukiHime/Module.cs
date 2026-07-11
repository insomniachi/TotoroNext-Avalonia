using System.Net;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.TsukiHime.ViewModels;
using TotoroNext.Anime.TsukiHime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("5078da1b-71b9-454f-904e-f6ced46bd28e");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "TsukiHime",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        TsukiHimeGroups.Descriptor = Descriptor;
        
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient($"{Id}-api", client =>
        {
            client.BaseAddress = new Uri(AnimeProvider.BaseUrl);
        });
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddTransient<IInitializer, Initializer>();
    }
}

public class Settings : OverridableConfig
{
    [IgnoreDataMember]
    public int Group { get; set; } = 10;
}