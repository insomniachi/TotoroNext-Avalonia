using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.ViewModels;
using TotoroNext.Anime.Views;
//using TotoroNext.Anime.Abstractions;
//using TotoroNext.Anime.Abstractions.Models;
//using TotoroNext.Anime.Services;
//using TotoroNext.Anime.UserInteractions;
//using TotoroNext.Anime.ViewModels;
//using TotoroNext.Anime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        //services.AddSingleton<IPlaybackProgressService, PlaybackProgressTrackingService>()
        //        .AddTransient<IAnimeThemes, AnimeThemes>();

        //// main navigation
        services.AddMainNavigationItem<SearchView, SearchViewModel>("Search",
            IconPacks.Avalonia.MaterialDesign.PackIconMaterialDesignKind.Search);

        //// Pane navigation
        //services.AddDataViewMap<AnimeDetailsView, AnimeDetailsViewModel, AnimeModel>()
        //        .AddDataViewMap<UserListFilterView, UserListFilterViewModel, UserListFilter>()
        //        .AddDataViewMap<AnimeEpisodesListView, AnimeEpisodesListViewModel, EpisodesListViewModelNagivationParameters>()
        //        .AddDataViewMap<AnimeGridView, AnimeGridViewModel, List<AnimeModel>>()
        //        .AddDataViewMap<AnimeOverridesView, AnimeOverridesViewModel, OverridesViewModelNavigationParameters>()
        //        .AddDataViewMap<AnimeSongsView, AnimeSongsViewModel, SongsViewModelNavigationParameters>();

        // services.AddSelectionUserInteraction<SelectProviderResult, SearchResult>()
        //         .AddSelectionUserInteraction<SelectAnimeResult, AnimeModel>()
        //         .AddSelectionUserInteraction<SelectServerResult, VideoServer>();

        services.AddHostedService<TrackingUpdater>()
                .AddHostedService(sp => sp.GetRequiredService<IPlaybackProgressService>());
    }
}
