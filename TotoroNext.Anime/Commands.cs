using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.ViewModels;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class Commands(
    IMessenger messenger,
    IAnimeOverridesRepository overridesRepository,
    IFactory<IAnimeProvider, Guid> providerFactory) : IInitializer
{
    public static ICommand? WatchCommand { get; private set; }
    public static ICommand? SettingsCommand { get; private set; }
    public static ICommand? DetailsCommand { get; private set; }

    public void Initialize()
    {
        WatchCommand = CreateWatchCommand();
        SettingsCommand = CreateSettingsCommand();
        DetailsCommand = CreateDetailsCommand();
    }

    private AsyncRelayCommand<AnimeModel> CreateWatchCommand()
    {
        return new AsyncRelayCommand<AnimeModel>(async anime =>
        {
            if (anime is null)
            {
                return;
            }

            var overrides = overridesRepository.GetOverrides(anime.Id);

            var provider = overrides?.Provider is { } providerId
                ? providerFactory.Create(providerId)
                : providerFactory.CreateDefault();

            var term = string.IsNullOrEmpty(overrides?.SelectedResult)
                ? anime.Title
                : overrides.SelectedResult;

            var result = await provider.SearchAndSelectAsync(term);

            if (overrides is not null)
            {
                messenger.Send(overrides);
            }

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
                ViewModel = typeof(AnimeOverridesViewModel),
                Title = anime.Title,
                Data = new OverridesViewModelNavigationParameters(anime),
                CloseButtonVisible = true,
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