using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AdvancedSearchViewModel(
    IFactory<IMetadataService, Guid> metadataFactory,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IMessenger messenger,
    IAnimeOverridesRepository overridesRepository) : ObservableObject, IInitializable
{
    private readonly IMetadataService _metadataService = metadataFactory.CreateDefault();

    [ObservableProperty] public partial AnimeSeason? Season { get; set; }
    [ObservableProperty] public partial int? Year { get; set; }
    [ObservableProperty] public partial int? MinimumScore { get; set; }
    [ObservableProperty] public partial int? MaximumScore { get; set; }
    [ObservableProperty] public partial string? Title { get; set; }
    [ObservableProperty] public partial List<AnimeModel> Anime { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<string> IncludedGenres { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<string> ExcludedGenres { get; set; } = [];

    public int CurrentYear { get; } = DateTime.Now.Year;

    public void Initialize()
    {
        var propertiesChanged = this.WhenAnyPropertyChanged(nameof(Title), nameof(Year), nameof(Season), nameof(MinimumScore), nameof(MaximumScore))
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
            .Select(_ => new AdvancedSearchRequest
            {
                Title = Title,
                SeasonName = Season,
                Year = Year,
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
    private async Task NavigateToWatch(AnimeModel anime)
    {
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
    }

    [RelayCommand]
    private void OpenAnimeDetails(AnimeModel anime)
    {
        messenger.Send(new PaneNavigateToDataMessage(anime, paneWidth: 750, title: anime.Title));
    }
}