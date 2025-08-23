using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.AnimeOnsen.ViewModels;
using TotoroNext.Anime.AnimeOnsen.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("c37d57cf-5090-4b48-977d-dbaa78545433"),
        Name = "Anime Onsen",
        HeroImage = ResourceHelper.GetResource("animeonsen.jpg"),
        Description =
            "Watch anime, always up to date and in high quality, with multiple sub direct from Japan The God of High School, Attack on Titan and more!",
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor)
                .AddModuleSettings(this)
                .AddTransient<IBackgroundInitializer, BackgroundInitializer>()
                .AddViewMap<SettingsView, SettingsViewModel>()
                .AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}

public class Settings
{
    public static readonly TaskCompletionSource<string> SearchTokenTaskCompletionSource = new();
    public string SubtitleLanguage { get; set; } = "en-US";
    public AnimeOnsenApiToken? ApiToken { get; set; }
}

[Serializable]
public class AnimeOnsenApiToken
{
    public string Token { get; set; } = "";
    public DateTime RenewTime { get; set; }
}