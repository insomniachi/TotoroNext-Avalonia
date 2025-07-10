using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

public partial class UserListViewModel(IFactory<ITrackingService, Guid> factory,
                                       IFactory<IAnimeProvider, Guid> providerFactory,
                                       IAnimeOverridesRepository animeOverridesRepository,
                                       IMessenger messenger) : ObservableObject, IAsyncInitializable
{
    private readonly ITrackingService? _trackingService = factory.CreateDefault();
    private readonly IMessenger _messenger = messenger;
    private IEnumerable<AnimeModel> _allItems = [];
    
    
    // Design Instance
    public UserListViewModel() : this(null!, null!, null!, null!)
    {
        
    }

    public UserListFilter Filter { get; } = new();

    public List<ListItemStatus> AllStatus { get; } = [ListItemStatus.Watching, ListItemStatus.PlanToWatch, ListItemStatus.Completed, ListItemStatus.OnHold];

    [ObservableProperty]
    public partial List<AnimeModel> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool IsPaneOpen { get; set; }


    public async Task InitializeAsync()
    {
        if (_trackingService is null)
        {
            return;
        }

        IsLoading = true;

        _allItems = await _trackingService.GetUserList();
        Items = [.. _allItems];

        IsLoading = false;

        _messenger.Register<ClosePaneMessage>(this, (_, _) => IsPaneOpen = false);

        Filter
            .WhenAnyValue(x => x.Year, x => x.Status, x => x.Term)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                Items = [.. _allItems.Where(Filter.IsVisible)];
            });
    }

    public async Task NavigateToWatch(AnimeModel anime)
    {
        var overrides = animeOverridesRepository.GetOverrides(anime.Id);

        var provider = overrides?.Provider is { } providerId
            ? providerFactory.Create(providerId)
            : providerFactory.CreateDefault();

        if (provider is null)
        {
            return;
        }

        var result = await provider.SearchAndSelectAsync(anime);

        if (overrides is not null)
        {
            _messenger.Send(overrides);
        }

        if (result is null)
        {
            return;
        }

        _messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(result, anime)));
    }

    public void OpenAnimeDetails(AnimeModel anime)
    {
        _messenger.Send(new PaneNavigateToDataMessage(anime, 750));
    }


    [RelayCommand]
    private void OpenFilterPane()
    {
        _messenger.Send(new PaneNavigateToDataMessage(Filter));
        IsPaneOpen = true;
    }

    [RelayCommand]
    private void ClearFilters() => Filter.Clear();
}

