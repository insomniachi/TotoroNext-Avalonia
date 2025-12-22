using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class SequelLocatorViewModel : ObservableObject, IAsyncInitializable, IDisposable, INavigatorHost
{
    private readonly ReadOnlyObservableCollection<AnimeModel> _anime;
    private readonly SourceCache<AnimeModel, long> _animeCache = new(x => x.Id);
    private readonly CancellationTokenSource _cts = new();
    private readonly ILocalTrackingService _localTrackingService;
    private readonly ITrackingService _trackingService;

    public SequelLocatorViewModel(IFactory<ITrackingService, Guid> trackingServiceFactory,
                                  ILocalTrackingService localTrackingService)
    {
        _localTrackingService = localTrackingService;
        _trackingService = trackingServiceFactory.CreateDefault()!;

        _animeCache
            .Connect()
            .RefCount()
            .AutoRefresh()
            .Filter(Filter.Predicate)
            .Sort(Sort.Comparer)
            .Bind(out _anime)
            .DisposeMany()
            .Subscribe();

        this.WhenAnyValue(x => x.Navigator)
            .WhereNotNull()
            .Subscribe(navigator => navigator.NavigateToData(new UserListSortAndFilter(Sort, Filter)));

        Filter.Format = AnimeMediaFormat.Tv;
    }

    public UserListFilter Filter { get; } = new();
    public UserListSort Sort { get; } = new();

    public ReadOnlyObservableCollection<AnimeModel> Anime => _anime;

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        var userlist = await _trackingService.GetUserList(_cts.Token);
        var sequelsAndPrequels = await _localTrackingService.GetPrequelsAndSequelsWithoutTracking(userlist, _cts.Token);
        _animeCache.AddOrUpdate(sequelsAndPrequels);
        IsLoading = false;
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    [ObservableProperty] public partial INavigator? Navigator { get; set; }
}