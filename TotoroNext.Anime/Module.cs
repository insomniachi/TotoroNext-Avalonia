using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
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
                .AddTransient<IAnimeMusicService, AnimeMusicService>()
                .AddSingleton<IAnimeRelations, AnimeRelations>()
                .AddSingleton<IDownloadManager, DownloadManager>()
                .AddDebrid()
                .AddDownloaders();

        // initializers
        services.AddTransient<IInitializer, AnimeRelationsParser>()
                .AddTransient<IInitializer, Commands>()
                .AddTransient<IBackgroundInitializer, AnimeRelationsParser>();

        // main navigation
        services.AddMainNavigationItem<HomeView, HomeViewModel>("Home", CommonIcons.Home);
        services.AddMainNavigationItem<UserListView, UserListViewModel>("Anime List", CommonIcons.List);
        services.AddMainNavigationItem<AdvancedSearchView, AdvancedSearchViewModel>("Search", CommonIcons.Search);
        services.AddMainNavigationItem<ProviderDebuggerView, ProviderDebuggerViewModel>("Browse", CommonIcons.Earth);
        services.AddMainNavigationItem<SequelLocatorView, SequelLocatorViewModel>("Missed", CommonIcons.NewReleases);
        AddDownloadsView(services);

        // Pane navigation
        services.AddDataViewMap<UserListSortAndFilterView, UserListSortAndFilterViewModel, UserListSortAndFilter>()
                .AddDataViewMap<AnimeDetailsView, AnimeDetailsViewModel, AnimeModel>()
                .AddDataViewMap<AnimeEpisodesListView, AnimeEpisodesListViewModel, EpisodesListViewModelNavigationParameters>()
                .AddDataViewMap<WatchView, WatchViewModel, WatchViewModelNavigationParameter>()
                .AddDataViewMap<AnimeGridView, AnimeGridViewModel, List<AnimeModel>>()
                .AddDataViewMap<AnimeExtensionsView, AnimeExtensionsViewModel, OverridesViewModelNavigationParameters>()
                .AddDataViewMap<AnimeSongsView, AnimeSongsViewModel, SongsViewModelNavigationParameters>()
                .AddDataViewMap<AnimeInfoView, AnimeInfoViewModel, InfoViewNavigationParameters>()
                .AddDataViewMap<AnimeCharactersView, AnimeCharactersViewModel, CharactersViewNavigationParameters>()
                .AddDataViewMap<TrailersView, TrailersViewModel, List<TrailerVideo>>();

        // dialogs
        services.AddDataViewMap<TorrentsView, TorrentsViewModel, TorrentsViewModelNavigationParameters>()
                .AddDataViewMap<RelationsBuilderView, RelationsBuilderViewModel, RelationsBuilderViewModelNavigationParameters>()
                .AddKeyedViewMap<DownloadRequestView, DownloadRequestViewModel>("Download");

        services.AddSelectionUserInteraction<SelectProviderResult, SearchResult>()
                .AddSelectionUserInteraction<SelectAnimeResult, AnimeModel>();

        services.AddHostedService(sp => sp.GetRequiredService<ITrackingUpdater>())
                .AddHostedService(sp => sp.GetRequiredService<IPlaybackProgressService>());
    }

    private static void AddDownloadsView(IServiceCollection services)
    {
        services.AddKeyedViewMap<DownloadsView, DownloadsViewModel>("Downloads");
        services.AddTransient(sp =>
        {
            var downloadManager = sp.GetRequiredService<IDownloadManager>();
            var item = new NavigationDrawerItem
            {
                Header = "Downloads",
                IconKey = CommonIcons.Downloads,
                Tag = new NavigationDrawerItemTag
                {
                    ViewModelType = typeof(DownloadsViewModel),
                    IsFooterItem = true
                }
            };

            downloadManager.DownloadsChanged += (_, _) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var count = downloadManager.Downloads.Count(x => !x.IsCompleted);
                    item.BadgeContent = count == 0 ? null : count.ToString();
                });
            };

            return item;
        });
    }
}