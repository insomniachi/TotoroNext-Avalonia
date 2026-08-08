using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Miruro.ViewModels;
using TotoroNext.Anime.Miruro.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miruro;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("df62dec6-ebe2-407e-9dea-854416fa23bd");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "Miruro",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("miruro.png")
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddHttpClient($"{Id}", client =>
        {
            client.BaseAddress = new Uri(AnimeProvider.BaseUrl);
        });
    }
}

public class Settings : OverridableConfig
{
    [DisplayName("Default Server")]
    [AllowedValues("kiwi", "pewe")]
    public string PreferredProvider { get; set; } = "kiwi";
    
    [DisplayName("Sub Type")]
    [AllowedValues("sub", "dub")]
    public string PreferredSubType { get; set; } = "sub";
}