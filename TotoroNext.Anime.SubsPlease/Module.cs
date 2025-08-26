using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.SubsPlease.ViewModels;
using TotoroNext.Anime.SubsPlease.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.SubsPlease;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("9b7a71c8-7633-46e8-be5e-288c19ac1330"),
        Name = "SubsPlease",
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("subsplease.jpg"),
        Description = "SubsPlease’s goal is to provide fast and timely English subtitled anime immediately after they are simulcasted. Above all," +
                      " we value consistency and we aim to fill in the void that was left by HorribleSubs. We want you to be able to come to our site " +
                      "and download seasonal anime right after they’re simulcasted. For easy downloading, we will also batch our shows at the end of " +
                      "the season for those who prefer to binge watch."
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddTransient<IInitializer, Initializer>();
        services.AddTransient<IBackgroundInitializer, Initializer>();
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}