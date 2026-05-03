using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AdvancedSearchViewModel : ObservableObject, IAsyncInitializable
{
    private bool _isChangeNotificationsEnabled = true;
    private readonly SourceCache<AnimeModel, long> _animeCache = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<AnimeModel> _anime;
    private readonly IFactory<IMetadataService, Guid> _metadataFactory;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IDialogService _dialogService;

    public AdvancedSearchViewModel(IFactory<IMetadataService, Guid> metadataFactory,
                                   IEnumerable<Descriptor> descriptors,
                                   ILocalSettingsService localSettingsService,
                                   IDialogService dialogService)
    {
        _metadataFactory = metadataFactory;
        _localSettingsService = localSettingsService;
        _dialogService = dialogService;
        
        _animeCache
            .Connect()
            .RefCount()
            .AutoRefresh()
            .Sort(Sort.Comparer)
            .Bind(out _anime)
            .DisposeMany()
            .Subscribe();
        
        MetadataServices = [Descriptor.Default, ..descriptors.Where(x => x.Components.Contains(ComponentTypes.Metadata))];
    }

    [ObservableProperty] public partial AnimeSeason? Season { get; set; }
    [ObservableProperty] public partial int? MinimumYear { get; set; }
    [ObservableProperty] public partial int? MaximumYear { get; set; } 
    [ObservableProperty] public partial float? MinimumScore { get; set; }
    [ObservableProperty] public partial float? MaximumScore { get; set; }
    [ObservableProperty] public partial string? Title { get; set; }
    [ObservableProperty] public partial ObservableCollection<string> IncludedGenres { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<string> ExcludedGenres { get; set; } = [];
    [ObservableProperty] public partial List<string> AllGenres { get; set; } = [];
    [ObservableProperty] public partial Descriptor? SelectedService { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    public ReadOnlyObservableCollection<AnimeModel> Anime => _anime;
    
    public UserListSort Sort { get; } = new()
    {
        Field = SortField.MeanScore,
        IsAscending = false,
    };

    public List<Descriptor> MetadataServices { get; }
    
    public List<AnimeSeason?> Seasons { get; set; } =
    [
        null, // Allow clearing the selection
        AnimeSeason.Winter,
        AnimeSeason.Spring,
        AnimeSeason.Summer,
        AnimeSeason.Fall
    ];

    public int CurrentYear { get; } = DateTime.Now.Year;

    public async Task InitializeAsync()
    {
        if (MetadataServices is { Count: 0 })
        {
            await _dialogService.Warning("No metadata services found. Please install at least one metadata service module.");
            return;
        }

        var defaultServiceId = _localSettingsService.ReadSetting<Guid?>("SelectedTrackingService");
        SelectedService = MetadataServices.FirstOrDefault(x => x.Id == defaultServiceId);

        var serviceChanged = this.WhenAnyValue(x => x.SelectedService)
                                 .WhereNotNull()
                                 .Select(s => _metadataFactory.Create(s.Id))
                                 .WhereNotNull()
                                 .Replay(1)
                                 .RefCount();

        serviceChanged.SelectMany(x => x.GetGenresAsync())
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(genres => AllGenres = genres);

        var propertiesChanged = this.WhenAnyPropertyChanged(nameof(MinimumYear),
                                                            nameof(Season),
                                                            nameof(MinimumScore),
                                                            nameof(MaximumScore),
                                                            nameof(MaximumYear))
                                    .Throttle(TimeSpan.FromMilliseconds(500))
                                    .Select(_ => Unit.Default);
        var titleChanged = this.WhenAnyValue(x => x.Title)
                               .Throttle(TimeSpan.FromSeconds(1))
                               .Select(_ => Unit.Default);

        var includedGenresChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                                   h => IncludedGenres.CollectionChanged += h,
                                                   h => IncludedGenres.CollectionChanged -= h)
                                              .Select(_ => Unit.Default);

        var excludedGenresChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                                   h => ExcludedGenres.CollectionChanged += h,
                                                   h => ExcludedGenres.CollectionChanged -= h)
                                              .Select(_ => Unit.Default);

        var trigger = Observable.Merge(titleChanged, propertiesChanged, includedGenresChanged, excludedGenresChanged);

        serviceChanged
            .Select(service =>
            {
                ClearSearchFilters();
                var metadataService = _metadataFactory.Create(service.Id);
                if (metadataService is null)
                {
                    return Observable.Empty<List<AnimeModel>>();
                }

                return trigger
                       .Where(_ => _isChangeNotificationsEnabled)
                       .Select(_ => new AdvancedSearchRequest
                       {
                           Title = Title,
                           SeasonName = Season,
                           MinYear = MinimumYear,
                           MaxYear = MaximumYear,
                           IncludedGenres = IncludedGenres.Count > 0 ? [..IncludedGenres] : null,
                           ExcludedGenres = ExcludedGenres.Count > 0 ? [..ExcludedGenres] : null,
                           MinimumScore = MinimumScore,
                           MaximumScore = MaximumScore
                       })
                       .Where(x => !x.IsEmpty())
                       .Do(_ => IsLoading = true)
                       .SelectMany(metadataService.SearchAnimeAsync);
            })
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(list =>
            {
                _animeCache.Clear();
                _animeCache.AddOrUpdate(list);
                IsLoading = false;
            });
    }

    [RelayCommand]
    private void ClearSearchFilters()
    {
        _isChangeNotificationsEnabled = false;

        Title = string.Empty;
        Season = null;
        MinimumYear = null;
        MaximumYear = null;
        IncludedGenres = [];
        ExcludedGenres = [];
        MinimumScore = null;
        MaximumScore = null;
        _animeCache.Clear();

        _isChangeNotificationsEnabled = true;
    }

    [RelayCommand]
    private void CurrentSeason()
    {
        _isChangeNotificationsEnabled = false;
        MinimumYear = MaximumYear = DateTime.Now.Year;
        _isChangeNotificationsEnabled = true;
        Season = AnimeHelpers.CurrentSeason().SeasonName;
    }
}