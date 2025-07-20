using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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
    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial string? SelectedResult { get; set; }

    [ObservableProperty] public partial List<SearchResult> ProviderResults { get; set; } = [];

    public List<Descriptor> Providers { get; } = [.. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeOverridesRepository.GetOverrides(parameters.Anime.Id);

        IsNsfw = overrides?.IsNsfw ?? false;
        ProviderId = overrides?.Provider;
        SelectedResult = overrides?.SelectedResult;

        this.WhenAnyPropertyChanged(nameof(IsNsfw), nameof(ProviderId), nameof(SelectedResult))
            .Select(_ => new AnimeOverrides()
            {
                IsNsfw = IsNsfw,
                Provider = ProviderId,
                SelectedResult = SelectedResult
            })
            .Subscribe(@override => animeOverridesRepository.CreateOrUpdate(parameters.Anime.Id, @override));

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .SelectMany(id =>
            {
                var provider = providerFactory.Create(id!.Value);
                return provider.SearchAsync(@parameters.Anime.Title).ToListAsync().AsTask();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                var currentResult = SelectedResult;
                ProviderResults = results;
                SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == currentResult)?.Title;
            });
    }
}