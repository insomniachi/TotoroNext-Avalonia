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
    private readonly CompositeDisposable _disposables = new();

    [ObservableProperty] public partial List<AnimeModel> Popular { get; set; } = [];

    [ObservableProperty] public partial List<AnimeModel> Upcoming { get; set; } = [];

    [ObservableProperty] public partial List<AnimeModel> HeroItems { get; set; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }
    
    [ObservableProperty] public partial int SelectedHeroIndex { get; set; }

    public async Task InitializeAsync()
    {
        IsLoading = true;

        Popular = await _metadataService.GetPopularAnimeAsync();
        Upcoming = await _metadataService.GetUpcomingAnimeAsync();

        var current = AnimeHelpers.CurrentSeason();
        HeroItems = Popular.OrderByDescending(x => x.MeanScore)
                           .Where(x => x.Season == current)
                           .Take(5)
                           .ToList();

        IsLoading = false;

        Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
                  .Select(_ => SelectedHeroIndex == HeroItems.Count - 1 ? 0 : SelectedHeroIndex + 1)
                  .ObserveOn(RxApp.MainThreadScheduler)
                  .Subscribe(index => SelectedHeroIndex = index)
                  .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}