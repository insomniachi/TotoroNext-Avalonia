using System.ComponentModel;
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
        nameof(OpeningSkipMethod),
        nameof(EndingSkipMethod),
        nameof(SearchTerm),
        nameof(ProviderOptions)
    ];

    private IAnimeProvider? _animeProvider;
    private bool _suppressProviderChange = true;
    private bool _isDeleting;

    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial List<string> ProviderResults { get; set; } = [];

    [ObservableProperty] public partial SkipMethod OpeningSkipMethod { get; set; }

    [ObservableProperty] public partial SkipMethod EndingSkipMethod { get; set; }

    [ObservableProperty] public partial string? SearchTerm { get; set; }

    [ObservableProperty] public partial bool HasProvider { get; set; }

    [ObservableProperty] public partial List<ModuleOptionItem> ProviderOptions { get; set; } = [];
    public List<Descriptor> Providers { get; } = [Descriptor.None, .. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeExtensionService.GetExtension(parameters.Anime.Id);
        SearchTerm = overrides?.SearchTerm;
        IsNsfw = overrides?.IsNsfw ?? false;
        _suppressProviderChange = true;
        ProviderId = overrides?.Provider;
        _suppressProviderChange = false;
        OpeningSkipMethod = overrides?.OpeningSkipMethod ?? SkipMethod.Ask;
        EndingSkipMethod = overrides?.EndingSkipMethod ?? SkipMethod.Ask;
        ProviderOptions = overrides?.AnimeProviderOptions ?? [];

        if (ProviderId is not null && ProviderId != Guid.Empty)
        {
            _animeProvider = providerFactory.Create(ProviderId.Value);
        }

        if (ProviderOptions is { Count: > 0 })
        {
            Subscribe(ProviderOptions);
        }

        this.WhenAnyPropertyChanged(ObservedProperties)
            .Where(_ => !_isDeleting)
            .Select(_ => new AnimeOverrides
            {
                IsNsfw = IsNsfw,
                Provider = ProviderId == Guid.Empty ? null : ProviderId,
                OpeningSkipMethod = OpeningSkipMethod,
                EndingSkipMethod = EndingSkipMethod,
                SearchTerm = SearchTerm,
                AnimeProviderOptions = ProviderOptions
            })
            .Subscribe(@override => animeExtensionService.CreateOrUpdateExtension(parameters.Anime.Id, @override));

        var providerIdStream = this.WhenAnyValue(x => x.ProviderId)
                                   .DistinctUntilChanged()
                                   .Publish()
                                   .RefCount();
        
        providerIdStream
            .Subscribe(x => HasProvider = x.HasValue && x.Value != Guid.Empty);

        providerIdStream
            .Where(x => x != Guid.Empty && x.HasValue)
            .Where(_ => !_suppressProviderChange)
            .Skip(1)
            .Subscribe(id =>
            {
                Unsubscribe(ProviderOptions);
                _animeProvider = providerFactory.Create(id!.Value);
                ProviderOptions = _animeProvider.GetOptions();
                Subscribe(ProviderOptions);
            });

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .DistinctUntilChanged()
            .Skip(1)
            .Where(x => x != Guid.Empty)
            .Subscribe(_ => SearchTerm = "");

        this.WhenAnyValue(x => x.SearchTerm)
            .Where(x => x is { Length: > 2 })
            .Where(_ => _animeProvider is not null)
            .SelectMany(term => _animeProvider!.SearchAsync(term!).ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results => ProviderResults = results.Select(x => x.Title).ToList());
        
        _suppressProviderChange = false;
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
        SearchTerm = "";
        OpeningSkipMethod = default;
        EndingSkipMethod = default;
        Unsubscribe(ProviderOptions);
        ProviderOptions = [];
        _isDeleting = false;
    }
}