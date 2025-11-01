using System.ComponentModel;
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
public partial class AnimeExtensionsViewModel(
    OverridesViewModelNavigationParameters parameters,
    IAnimeExtensionService animeExtensionService,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IEnumerable<Descriptor> descriptors) : ObservableObject, IInitializable
{
    private static readonly string[] ObservedProperties =
    [
        nameof(IsNsfw),
        nameof(ProviderId),
        nameof(SelectedResult),
        nameof(OpeningSkipMethod),
        nameof(EndingSkipMethod),
        nameof(SearchTerm),
        nameof(ProviderOptions)
    ];

    private bool _isDeleting;

    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial string? SelectedResult { get; set; }

    [ObservableProperty] public partial List<SearchResult> ProviderResults { get; set; } = [];

    [ObservableProperty] public partial SkipMethod OpeningSkipMethod { get; set; }

    [ObservableProperty] public partial SkipMethod EndingSkipMethod { get; set; }

    [ObservableProperty] public partial string? SearchTerm { get; set; }

    [ObservableProperty] public partial bool HasProvider { get; set; }

    [ObservableProperty] public partial List<ModuleOptionItem> ProviderOptions { get; set; } = [];
    public List<Descriptor> Providers { get; } = [Descriptor.None, .. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeExtensionService.GetExtension(parameters.Anime.Id);

        SearchTerm = overrides?.SearchTerm ?? parameters.Anime.Title;
        IsNsfw = overrides?.IsNsfw ?? false;
        ProviderId = overrides?.Provider;
        SelectedResult = overrides?.SelectedResult;
        OpeningSkipMethod = overrides?.OpeningSkipMethod ?? SkipMethod.Ask;
        EndingSkipMethod = overrides?.EndingSkipMethod ?? SkipMethod.Ask;
        ProviderOptions = overrides?.AnimeProviderOptions ?? [];

        if (ProviderOptions is { Count: > 0})
        {
            Subscribe(ProviderOptions); 
        }

        this.WhenAnyPropertyChanged(ObservedProperties)
            .Where(_ => !_isDeleting)
            .Select(_ => new AnimeOverrides
            {
                IsNsfw = IsNsfw,
                Provider = ProviderId == Guid.Empty ? null : ProviderId,
                SelectedResult = SelectedResult,
                OpeningSkipMethod = OpeningSkipMethod,
                EndingSkipMethod = EndingSkipMethod,
                SearchTerm = SearchTerm,
                AnimeProviderOptions = ProviderOptions
            })
            .Subscribe(@override => animeExtensionService.CreateOrUpdateExtension(parameters.Anime.Id, @override));

        this.WhenAnyValue(x => x.ProviderId)
            .Select(x => x.HasValue || x != Guid.Empty)
            .Subscribe(value => HasProvider = value);

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .DistinctUntilChanged()
            .Skip(1)
            .Subscribe(id =>
            {
                Unsubscribe(ProviderOptions);
                var provider = providerFactory.Create(id!.Value);
                ProviderOptions = provider.GetOptions();
                Subscribe(ProviderOptions);
            });

        var providerIdChanged = this.WhenAnyValue(x => x.ProviderId)
                                    .Skip(1)
                                    .WhereNotNull()
                                    .Where(x => x != Guid.Empty)
                                    .Select(_ => Unit.Default);

        var searchTermChanged = this.WhenAnyValue(x => x.SearchTerm)
                                    .Skip(1)
                                    .Where(x => x is { Length: > 2 })
                                    .Where(_ => ProviderId.HasValue)
                                    .Select(_ => Unit.Default);

        providerIdChanged.Merge(searchTermChanged)
                         .SelectMany(_ =>
                         {
                             var provider = providerFactory.Create(ProviderId!.Value);
                             return provider.SearchAsync(SearchTerm).ToListAsync().AsTask();
                         })
                         .ObserveOn(RxApp.MainThreadScheduler)
                         .Subscribe(results =>
                         {
                             ProviderResults = results;
                             SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == overrides?.SelectedResult)?.Title;
                         });
    }
    
    private void Unsubscribe(List<ModuleOptionItem> providerOptions)
    {
        foreach (var item in providerOptions)
        {
            item.PropertyChanged -= RaiseProviderOptionChange;
        }
    }
    
    private void Subscribe(List<ModuleOptionItem> providerOptions)
    {
        foreach (var item in providerOptions)
        {
            item.PropertyChanged += RaiseProviderOptionChange;
        }
    }

    private void RaiseProviderOptionChange(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ProviderOptions));
    }

    [RelayCommand]
    private void Delete()
    {
        animeExtensionService.RemoveExtension(parameters.Anime.Id);

        _isDeleting = true;

        IsNsfw = false;
        ProviderId = null;
        SelectedResult = null;
        OpeningSkipMethod = default;
        EndingSkipMethod = default;

        _isDeleting = false;
    }
}