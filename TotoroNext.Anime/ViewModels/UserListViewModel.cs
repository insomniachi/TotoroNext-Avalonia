using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class UserListViewModel : ObservableObject, IAsyncInitializable
{
    private readonly ReadOnlyObservableCollection<AnimeModel> _anime;
    private readonly SourceCache<AnimeModel, long> _animeCache = new(x => x.Id);
    private readonly IMessenger _messenger;
    private readonly ITrackingService? _trackingService;
    private IEnumerable<AnimeModel> _allItems = [];

    public UserListViewModel(IFactory<ITrackingService, Guid> factory,
                             IMessenger messenger)
    {
        _messenger = messenger;
        _trackingService = factory.CreateDefault();

        _animeCache
            .Connect()
            .RefCount()
            .AutoRefresh()
            .Filter(Filter.Predicate)
            .Sort(Sort.Comparer)
            .Bind(out _anime)
            .DisposeMany()
            .Subscribe();
    }

    public UserListFilter Filter { get; } = new();
    public UserListSort Sort { get; } = new();

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
    private void OpenFilterPane()
    {
        _messenger.Send(new PaneNavigateToDataMessage(new UserListSortAndFilter(Sort, Filter), null!));
    }

    [RelayCommand]
    private void ClearFilters()
    {
        Filter.Clear();
    }
}

public record UserListSortAndFilter(UserListSort Sort, UserListFilter Filter);