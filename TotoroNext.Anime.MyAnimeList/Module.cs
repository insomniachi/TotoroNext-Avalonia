using IconPacks.Avalonia.Material;
using MalApi;
using MalApi.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.ViewModels;
using TotoroNext.Anime.Abstractions.Views;
using TotoroNext.Anime.MyAnimeList.ViewModels;
using TotoroNext.Anime.MyAnimeList.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList;

public class Module : IModule<Settings>
{
    public static Guid Id { get; } = new("e6b48ed5-4b76-4a7e-94d1-285c6dd4a125");

    public Descriptor Descriptor { get; } = new()
    {
        Id = Id,
        Name = "MyAnimeList",
        Components = [ComponentTypes.Metadata, ComponentTypes.Tracking],
        Description =
            "MyAnimeList, often abbreviated as MAL, is an anime and manga social networking and social cataloging application website run by volunteers. The site provides its users with a list-like system to organize and score anime and manga. It facilitates finding users who share similar tastes and provides a large database on anime and manga",
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("mal.jpg")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedViewMap<UpdateTrackingView, UpdateTrackingViewModel>($"tracking/{nameof(ExternalIds.MyAnimeList)}");
        services.AddChildNavigationViewItem<AnidleSolverView, AnidleSolverViewModel>("AniGuesser", "Anidle", PackIconMaterialKind.Puzzle);

        services.AddSingleton<IMalClient, MalClient>();
        services.AddKeyedTransient<IMetadataService, MyAnimeListMetadataService>(Descriptor.Id);
        services.AddKeyedTransient<ITrackingService, MyAnimeListTrackingService>(Descriptor.Id);
        services.AddTransient<IBackgroundInitializer, Initializer>();
    }
}

public class Settings
{
    public const string ClientId = "748da32a6defdd448c1f47d60b4bbe69";
    public OAuthToken? Auth { get; set; }
    public bool IncludeNsfw { get; set; }
    public int SearchLimit { get; set; } = 15;
}