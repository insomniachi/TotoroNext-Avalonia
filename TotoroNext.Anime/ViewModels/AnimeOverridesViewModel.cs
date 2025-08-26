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
public partial class AnimeOverridesViewModel(
    OverridesViewModelNavigationParameters parameters,
    IAnimeOverridesRepository animeOverridesRepository,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IEnumerable<Descriptor> descriptors) : ObservableObject, IInitializable
{
    private bool _isDeleting;
    
    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial string? SelectedResult { get; set; }

    [ObservableProperty] public partial List<SearchResult> ProviderResults { get; set; } = [];
    
    [ObservableProperty] public partial SkipMethod OpeningSkipMethod { get; set; }
    
    [ObservableProperty] public partial SkipMethod EndingSkipMethod { get; set; }

    [ObservableProperty] public partial string? SearchTerm { get; set; }
    public List<Descriptor> Providers { get; } = [ Descriptor.Empty, .. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeOverridesRepository.GetOverrides(parameters.Anime.Id);
        
        SearchTerm = overrides?.SearchTerm ?? parameters.Anime.Title;
        IsNsfw = overrides?.IsNsfw ?? false;
        ProviderId = overrides?.Provider;
        SelectedResult = overrides?.SelectedResult;
        OpeningSkipMethod = overrides?.OpeningSkipMethod ?? SkipMethod.Ask;
        EndingSkipMethod = overrides?.EndingSkipMethod ?? SkipMethod.Ask;

        this.WhenAnyPropertyChanged()
            .Where(_ => !_isDeleting)
            .Select(_ => new AnimeOverrides
            {
                IsNsfw = IsNsfw,
                Provider = ProviderId == Guid.Empty ? null: ProviderId,
                SelectedResult = SelectedResult,
                OpeningSkipMethod = OpeningSkipMethod,
                EndingSkipMethod = EndingSkipMethod,
                SearchTerm = SearchTerm
            })
            .Subscribe(@override => animeOverridesRepository.CreateOrUpdate(parameters.Anime.Id, @override));

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .Where(x => x != Guid.Empty)
            .SelectMany(id =>
            {
                var provider = providerFactory.Create(id!.Value);
                return provider.SearchAsync(SearchTerm).ToListAsync().AsTask();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                var currentResult = SelectedResult;
                ProviderResults = results;
                SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == currentResult)?.Title;
            });
        
        this.WhenAnyValue(x => x.SearchTerm)
            .Skip(1)
            .Where(x => x is {Length: > 2})
            .Where(_ => ProviderId.HasValue)
            .SelectMany(term =>
            {
                var provider = providerFactory.Create(ProviderId!.Value);
                return provider.SearchAsync(term ?? "").ToListAsync().AsTask();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                var currentResult = SelectedResult;
                ProviderResults = results;
                SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == currentResult)?.Title;
            });
    }

    [RelayCommand]
    private void Delete()
    {
        animeOverridesRepository.Remove(parameters.Anime.Id);
        
        _isDeleting = true;
        
        IsNsfw = false;
        ProviderId = null;
        SelectedResult = null;
        OpeningSkipMethod = default;
        EndingSkipMethod = default;

        _isDeleting = false;
    }
}