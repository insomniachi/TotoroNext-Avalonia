using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class UserListViewModel : ObservableObject, IAsyncInitializable
{
    private readonly ReadOnlyObservableCollection<AnimeModel> _anime;
    private readonly SourceCache<AnimeModel, long> _animeCache = new(x => x.Id);
    private readonly IAnimeOverridesRepository _animeOverridesRepository;
    private readonly IMessenger _messenger;
    private readonly IFactory<IAnimeProvider, Guid> _providerFactory;
    private readonly ITrackingService? _trackingService;
    private IEnumerable<AnimeModel> _allItems = [];

    public UserListViewModel(IFactory<ITrackingService, Guid> factory,
                             IFactory<IAnimeProvider, Guid> providerFactory,
                             IAnimeOverridesRepository animeOverridesRepository,
                             IMessenger messenger)
    {
        _providerFactory = providerFactory;
        _animeOverridesRepository = animeOverridesRepository;
        _messenger = messenger;
        _trackingService = factory.CreateDefault();

        _animeCache
            .Connect()
            .RefCount()
            .Filter(Filter.WhenAnyPropertyChanged().Select(x => (Func<AnimeModel, bool>)x!.IsVisible))
            .Bind(out _anime)
            .DisposeMany()
            .Subscribe();
    }

    public UserListFilter Filter { get; } = new();

    public List<ListItemStatus> AllStatus { get; } =
        [ListItemStatus.Watching, ListItemStatus.PlanToWatch, ListItemStatus.Completed, ListItemStatus.OnHold];

    [ObservableProperty] public partial List<AnimeModel> Items { get; set; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public ReadOnlyObservableCollection<AnimeModel> Anime => _anime;


    public async Task InitializeAsync()
    {
        if (_trackingService is null)
        {
            return;
        }

        IsLoading = true;

        _allItems = await _trackingService.GetUserList();
        _animeCache.AddOrUpdate(_allItems);
        Filter.Refresh();

        IsLoading = false;
    }

    [RelayCommand]
    private async Task NavigateToWatch(AnimeModel anime)
    {
        var overrides = _animeOverridesRepository.GetOverrides(anime.Id);

        var provider = overrides?.Provider is { } providerId
            ? _providerFactory.Create(providerId)
            : _providerFactory.CreateDefault();

        var term = string.IsNullOrEmpty(overrides?.SelectedResult)
            ? anime.Title
            : overrides.SelectedResult;
        
        var result = await provider.SearchAndSelectAsync(term);

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

    [RelayCommand]
    private void OpenAnimeDetails(AnimeModel anime)
    {
        _messenger.Send(new PaneNavigateToDataMessage(anime, paneWidth: 750, title: anime.Title));
    }


    [RelayCommand]
    private void OpenFilterPane()
    {
        _messenger.Send(new PaneNavigateToDataMessage(Filter, "Filter"));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        Filter.Clear();
    }
}