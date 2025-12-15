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
    private readonly CompositeDisposable _disposables = new();
    private readonly IMetadataService _metadataService = metadataFactory.CreateDefault()!;

    [ObservableProperty] public partial List<AnimeModel> HeroItems { get; set; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial int SelectedHeroIndex { get; set; }

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulatePopular { get; set; }

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulateUpcoming { get; set; }

    public Task InitializeAsync()
    {
        PopulatePopular = async () =>
        {
            var popular = await _metadataService.GetPopularAnimeAsync();
            var current = AnimeHelpers.CurrentSeason();
            HeroItems = popular.OrderByDescending(x => x.MeanScore)
                               .Where(x => x.Season == current)
                               .Take(5)
                               .ToList();
            return popular;
        };
        
        PopulateUpcoming = () => _metadataService.GetUpcomingAnimeAsync();

        Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
                  .Select(_ => SelectedHeroIndex == HeroItems.Count - 1 ? 0 : SelectedHeroIndex + 1)
                  .ObserveOn(RxApp.MainThreadScheduler)
                  .Subscribe(index => SelectedHeroIndex = index)
                  .DisposeWith(_disposables);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}