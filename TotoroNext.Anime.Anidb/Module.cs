using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Anidb.ViewModels;
using TotoroNext.Anime.Anidb.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anidb;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("79cf9217-e30e-4451-97f4-404ddae85d06");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "AniDB",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("anidb.png")
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
                    client.DefaultRequestHeaders.Referrer = new Uri(AnimeProvider.BaseUrl);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    CheckCertificateRevocationList = true
                });
    }
}

public class Settings : OverridableConfig
{
    [DisplayName("Audio Language")]
    [AllowedValues("English", "Japanese")]
    public string AudioLanguage { get; set; } = "Japanese";
}