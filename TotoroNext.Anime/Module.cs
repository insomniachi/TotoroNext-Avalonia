using IconPacks.Avalonia.MaterialDesign;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.ViewModels;
using TotoroNext.Anime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPlaybackProgressService, PlaybackProgressTrackingService>();
                // .AddTransient<IAnimeThemes, AnimeThemes>();

        //// main navigation
        services.AddMainNavigationItem<SearchView, SearchViewModel>("Search",
                                                                    PackIconMaterialDesignKind.Search);
        services.AddMainNavigationItem<UserListView, UserListViewModel>("My List",
                                                                        PackIconMaterialDesignKind.VideoLibrary);

        //// Pane navigation
        services.AddDataViewMap<UserListFilterView, UserListFilterViewModel, UserListFilter>()
                .AddDataViewMap<AnimeDetailsView, AnimeDetailsViewModel, AnimeModel>()
                .AddDataViewMap<AnimeEpisodesListView, AnimeEpisodesListViewModel, EpisodesListViewModelNagivationParameters>()
                .AddDataViewMap<WatchView, WatchViewModel, WatchViewModelNavigationParameter>();

        // services.AddSelectionUserInteraction<SelectProviderResult, SearchResult>()
        //         .AddSelectionUserInteraction<SelectAnimeResult, AnimeModel>()
        //         .AddSelectionUserInteraction<SelectServerResult, VideoServer>();

        services.AddHostedService<TrackingUpdater>()
                .AddHostedService(sp => sp.GetRequiredService<IPlaybackProgressService>());
    }
}