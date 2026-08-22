using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI.Reactive;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class SequelLocatorViewModel : ObservableObject, IInitializable, IDisposable, INavigatorHost
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
            .SortAndBind(out _anime, Sort.Comparer)
            .DisposeMany()
            .Subscribe();

        this.WhenAnyValue(x => x.Navigator)
            .WhereNotNull()
            .Subscribe(navigator => navigator.NavigateToData(new UserListSortAndFilter(Sort, Filter)));

        Filter.IsUserScoreFilterVisible = false;
        Filter.AllowUntracked = true;
        Sort.IsUserScoreSortVisible = false;
        Sort.IsDateCompletedSortVisible = false;
    }

    public UserListFilter Filter { get; } = new();
    public UserListSort Sort { get; } = new();

    public ReadOnlyObservableCollection<AnimeModel> Anime => _anime;

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public void Initialize()
    {
        IsLoading = true;
        _ = Task.Run(async () =>
        {
            var userlist = await _trackingService.GetUserList(_cts.Token);
            var sequelsAndPrequels = await _localTrackingService.GetPrequelsAndSequelsWithoutTracking(userlist, _cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _animeCache.AddOrUpdate(sequelsAndPrequels);
                IsLoading = false;
            });
        });
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    [ObservableProperty] public partial INavigator? Navigator { get; set; }
}