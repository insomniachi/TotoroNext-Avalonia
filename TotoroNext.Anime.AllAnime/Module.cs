using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.AllAnime.ViewModels;
using TotoroNext.Anime.AllAnime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("489576c5-2879-493b-874a-7eb14e081280"),
        Name = "AllAnime",
        Description =
            "AllAnime's goal is to provide you with the highest possible amount of daily anime episodes/manga chapters for free and without any kind of limitation.",
        HeroImage = ResourceHelper.GetResource("hero.png"),
        Components = [ComponentTypes.AnimeProvider, ComponentTypes.AnimeDownloader],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddViewMap<SettingsView, SettingsViewModel>();
    }
}

public class Settings
{
    public TranslationType TranslationType { get; set; } = TranslationType.Sub;
}

public enum TranslationType
{
    Sub,
    Dub,
    Raw
}