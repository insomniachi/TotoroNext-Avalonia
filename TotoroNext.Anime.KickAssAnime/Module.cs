using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.KickAssAnime.ViewModels;
using TotoroNext.Anime.KickAssAnime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.KickAssAnime;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("c2431b4e-98bc-41ef-9b8f-7429e62f9a76");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "KickAssAnime",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("kaa.png")
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
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);
            client.DefaultRequestHeaders.Referrer = new Uri(AnimeProvider.BaseUrl);
        });
    }
}

public class Settings : OverridableConfig
{

    [DisplayName("Audio Language")]
    [AllowedValues("en-US", "ja-JP")]
    public string AudioLanguage { get; set; } = "ja-JP";

    [DisplayName("Subtitle Language")]
    [AllowedValues("English", "Portuguese", "Spanish", "Arabic", "French", "German", "Italian", "Russian")]
    public string SubtitleLanguage { get; set; } = "English";
}