using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.ViewModels;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class Commands(
    IMessenger messenger,
    ITrackingUpdater trackingUpdater,
    IAnimeExtensionService extensionService) : IInitializer
{
    public static ICommand? WatchCommand { get; private set; }
    public static ICommand? SettingsCommand { get; private set; }
    public static ICommand? DetailsCommand { get; private set; }
    public static ICommand? SearchTorrentsCommand { get; private set; }
    public static ICommand? AddToListCommand { get; private set; }

    public void Initialize()
    {
        WatchCommand = CreateWatchCommand();
        SettingsCommand = CreateSettingsCommand();
        DetailsCommand = CreateDetailsCommand();
        SearchTorrentsCommand = CreateSearchTorrentsCommand();
        AddToListCommand = CreateAddToListCommand();
    }

    private AsyncRelayCommand<AnimeModel> CreateAddToListCommand()
    {
        return new AsyncRelayCommand<AnimeModel>(async anime =>
        {
            if (anime is null)
            {
                return;
            }
            
            await trackingUpdater.UpdateTracking(anime, new Tracking { Status = ListItemStatus.PlanToWatch });
        });
    }

    private RelayCommand<AnimeModel> CreateSearchTorrentsCommand()
    {
        return new RelayCommand<AnimeModel>(anime =>
        {
            if (anime is null)
            {
                return;
            }

            messenger.Send(new NavigateToViewModelDialogMessage
            {
                Title = "Torrents",
                Data = new TorrentsViewModelNavigationParameters(anime),
                ViewModel = typeof(TorrentsViewModel),
                CloseButtonVisible = true
            });
        });
    }

    private AsyncRelayCommand<AnimeModel> CreateWatchCommand()
    {
        return new AsyncRelayCommand<AnimeModel>(async anime =>
        {
            if (anime is null)
            {
                return;
            }

            var result = await extensionService.SearchAndSelectAsync(anime);

            if (result is null)
            {
                return;
            }

            messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(result, anime)));
        });
    }

    private RelayCommand<AnimeModel> CreateSettingsCommand()
    {
        return new RelayCommand<AnimeModel>(anime =>
        {
            if (anime is null)
            {
                return;
            }

            messenger.Send(new NavigateToViewModelDialogMessage
            {
                ViewModel = typeof(AnimeExtensionsViewModel),
                Title = anime.Title,
                Data = new OverridesViewModelNavigationParameters(anime),
                CloseButtonVisible = true
            });
        });
    }

    private RelayCommand<AnimeModel> CreateDetailsCommand()
    {
        return new RelayCommand<AnimeModel>(anime =>
        {
            if (anime is null)
            {
                return;
            }

            messenger.Send(new PaneNavigateToDataMessage(anime, paneWidth: 750, title: anime.Title));
        });
    }
}