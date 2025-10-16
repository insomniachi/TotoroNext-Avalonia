﻿using System.Collections.ObjectModel;
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
    IFactory<IMetadataService, Guid> metadataFactory,
    IEnumerable<Descriptor> descriptors,
    ILocalSettingsService localSettingsService,
    IDialogService dialogService) : ObservableObject, IAsyncInitializable
{
    private bool _isChangeNotificationsEnabled = true;
    private IMetadataService? _metadataService;

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
    [ObservableProperty] public partial Descriptor? SelectedService { get; set; }

    public List<Descriptor> MetadataServices { get; } = [..descriptors.Where(x => x.Components.Contains(ComponentTypes.Metadata))];

    public int CurrentYear { get; } = DateTime.Now.Year;

    public async Task InitializeAsync()
    {
        if (MetadataServices is { Count: 0 })
        {
            await dialogService.Warning("No metadata services found. Please install at least one metadata service module.");
            return;
        }
        
        var defaultServiceId = localSettingsService.ReadSetting<Guid?>("SelectedTrackingService");
        SelectedService = MetadataServices.FirstOrDefault(x => x.Id == defaultServiceId);

        this.WhenAnyValue(x => x.SelectedService)
            .WhereNotNull()
            .Do(service => _metadataService = metadataFactory.Create(service.Id))
            .SelectMany(_ => _metadataService!.GetGenresAsync())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(genres => AllGenres = genres);

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
            .Where(_ => _metadataService is not null)
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
            .SelectMany(_metadataService!.SearchAnimeAsync)
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