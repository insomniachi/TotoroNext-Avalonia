using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AdvancedSearchViewModel(
    IFactory<IMetadataService, Guid> metadataFactory) : ObservableObject, IAsyncInitializable
{
    private readonly IMetadataService _metadataService = metadataFactory.CreateDefault();
    private bool _isChangeNotificationsEnabled = true;

    [ObservableProperty] public partial AnimeSeason? Season { get; set; }
    [ObservableProperty] public partial int? MinimumYear { get; set; }
    [ObservableProperty] public partial int? MaximumYear { get; set; }
    [ObservableProperty] public partial float? MinimumScore { get; set; }
    [ObservableProperty] public partial float? MaximumScore { get; set; }
    [ObservableProperty] public partial string? Title { get; set; }
    [ObservableProperty] public partial List<AnimeModel> Anime { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<string> IncludedGenres { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<string> ExcludedGenres { get; set; } = [];
    [ObservableProperty] public partial List<string> AllGenres { get; set; } = [];

    public int CurrentYear { get; } = DateTime.Now.Year;

    public async Task InitializeAsync()
    {
        AllGenres = await _metadataService.GetGenresAsync();

        var propertiesChanged = this.WhenAnyPropertyChanged(nameof(Title),
                                                            nameof(MinimumYear),
                                                            nameof(Season),
                                                            nameof(MinimumScore),
                                                            nameof(MaximumScore),
                                                            nameof(MaximumYear))
                                    .Select(_ => Unit.Default);

        var includedGenresChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                                   h => IncludedGenres.CollectionChanged += h,
                                                   h => IncludedGenres.CollectionChanged -= h)
                                              .Select(_ => Unit.Default);

        var excludedGenresChanged = Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                                   h => ExcludedGenres.CollectionChanged += h,
                                                   h => ExcludedGenres.CollectionChanged -= h)
                                              .Select(_ => Unit.Default);

        var trigger = Observable.Merge(propertiesChanged, includedGenresChanged, excludedGenresChanged);

        trigger
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
            .SelectMany(_metadataService.SearchAnimeAsync)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(list => Anime = list);
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
        Anime = [];

        _isChangeNotificationsEnabled = true;
    }
}