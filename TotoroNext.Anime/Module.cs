using IconPacks.Avalonia.MaterialDesign;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Interaction;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.ViewModels;
using TotoroNext.Anime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPlaybackProgressService, PlaybackProgressTrackingService>()
                .AddSingleton<ITrackingUpdater, TrackingUpdater>()
                .AddTransient<IAnimeThemes, AnimeThemes>()
                .AddSingleton<IAnimeRelations, AnimeRelations>()
                .AddTransient<IInitializer, AnimeRelationsParser>()
                .AddTransient<IBackgroundInitializer, AnimeRelationsParser>();

        // main navigation
        services.AddMainNavigationItem<UserListView, UserListViewModel>("Anime List", PackIconMaterialDesignKind.List);
        services.AddMainNavigationItem<AdvancedSearchView, AdvancedSearchViewModel>("Search", PackIconMaterialDesignKind.Search);

        // Pane navigation
        services.AddDataViewMap<UserListSortAndFilterView, UserListSortAndFilterViewModel, UserListSortAndFilter>()
                .AddDataViewMap<AnimeDetailsView, AnimeDetailsViewModel, AnimeModel>()
                .AddDataViewMap<AnimeEpisodesListView, AnimeEpisodesListViewModel, EpisodesListViewModelNavigationParameters>()
                .AddDataViewMap<WatchView, WatchViewModel, WatchViewModelNavigationParameter>()
                .AddDataViewMap<AnimeGridView, AnimeGridViewModel, List<AnimeModel>>()
                .AddDataViewMap<AnimeOverridesView, AnimeOverridesViewModel, OverridesViewModelNavigationParameters>()
                .AddDataViewMap<AnimeSongsView, AnimeSongsViewModel, SongsViewModelNavigationParameters>()
                .AddDataViewMap<AnimeInfoView, AnimeInfoViewModel, InfoViewNavigationParameters>();

        services.AddSelectionUserInteraction<SelectProviderResult, SearchResult>()
                .AddSelectionUserInteraction<SelectAnimeResult, AnimeModel>();

        services.AddHostedService(sp => sp.GetRequiredService<ITrackingUpdater>())
                .AddHostedService(sp => sp.GetRequiredService<IPlaybackProgressService>());
    }
}