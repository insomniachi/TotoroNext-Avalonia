using System.Collections.ObjectModel;
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
        nameof(ProviderOptions),
        nameof(ProviderResult)
    ];

    private IAnimeProvider? _animeProvider;
    private bool _isDeleting;
    private bool _suppressProviderChange = true;

    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial SkipMethod OpeningSkipMethod { get; set; }

    [ObservableProperty] public partial SkipMethod EndingSkipMethod { get; set; }

    [ObservableProperty] public partial string? SearchTerm { get; set; }

    [ObservableProperty] public partial bool HasProvider { get; set; }

    [ObservableProperty] public partial List<ModuleOptionItem> ProviderOptions { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<string> ProviderResults { get; set; } = [];
    
    [ObservableProperty] public partial SearchResult? ProviderResult { get; set; }

    public List<Descriptor> Providers { get; } = [Descriptor.None, .. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeExtensionService.GetExtension(parameters.Anime.Id);
        SearchTerm = overrides?.ProviderResult is { } pr ? pr.Title : null;
        IsNsfw = overrides?.IsNsfw ?? false;
        _suppressProviderChange = true;
        ProviderId = overrides?.Provider;
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
            .Select(_ =>
            {
                var extensions = new AnimeOverrides
                {
                    IsNsfw = IsNsfw,
                    Provider = ProviderId == Guid.Empty ? null : ProviderId,
                    OpeningSkipMethod = OpeningSkipMethod,
                    EndingSkipMethod = EndingSkipMethod,
                    AnimeProviderOptions = ProviderOptions,
                };

                if (ProviderId != null && ProviderResult is not null)
                {
                    extensions.ProviderResult = new ProviderItemResult()
                    {
                        Id = ProviderResult.Id,
                        Title = ProviderResult.Title
                    };
                }

                return extensions;
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

        _suppressProviderChange = false;
    }
    
    public async Task<List<SearchResult>> GetSearchResults(string? term)
    {
        if (_animeProvider is null || string.IsNullOrEmpty(term))
        {
            return [];
        }

        try
        {
            return await _animeProvider.SearchAsync(term).ToListAsync();
        }
        catch
        {
            return [];
        }
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
        ProviderResult = null;
        Unsubscribe(ProviderOptions);
        ProviderOptions = [];

        _isDeleting = false;
    }
}