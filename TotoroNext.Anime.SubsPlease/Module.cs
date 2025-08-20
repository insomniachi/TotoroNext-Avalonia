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
        HeroImage = ResourceHelper.GetResource("subsplease.jpg")
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