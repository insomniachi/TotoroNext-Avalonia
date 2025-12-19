using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class HomeViewModel(IFactory<IMetadataService, Guid> metadataFactory) : ObservableObject, IAsyncInitializable, IDisposable
{
    private readonly IMetadataService _metadataService = metadataFactory.CreateDefault()!;
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty] public partial List<AnimeModel> HeroItems { get; set; } = [];

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulatePopular { get; set; }

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulateUpcoming { get; set; }
    
    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulateAiringToday { get; set; }

    public Task InitializeAsync()
    {
        PopulatePopular = async () =>
        {
            var popular = await _metadataService.GetPopularAnimeAsync(_cts.Token);
            var current = AnimeHelpers.CurrentSeason();
            HeroItems = popular.OrderByDescending(x => x.MeanScore)
                               .Where(x => x.Season == current)
                               .Take(5)
                               .ToList();
            return popular;
        };
        
        PopulateUpcoming = () => _metadataService.GetUpcomingAnimeAsync(_cts.Token);
        PopulateAiringToday = () => _metadataService.GetAiringToday(_cts.Token);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}