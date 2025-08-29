using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeDetailsViewModel(
    AnimeModel anime,
    IFactory<IMetadataService, Guid> metaFactory,
    ITrackingUpdater trackingUpdater) : ObservableObject, IAsyncInitializable, INavigatorHost
{
    private readonly IMetadataService _metadataService = metaFactory.CreateDefault();

    [ObservableProperty] public partial AnimeModel Anime { get; set; } = anime;

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = anime.Tracking?.Status;

    [ObservableProperty] public partial int? Progress { get; set; } = anime.Tracking?.WatchedEpisodes;

    [ObservableProperty] public partial int? Score { get; set; } = anime.Tracking?.Score;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; } = anime.Tracking?.StartDate == new DateTime() ? null : anime.Tracking?.StartDate;

    [ObservableProperty]
    public partial DateTime? FinishDate { get; set; } = anime.Tracking?.FinishDate == new DateTime() ? null : anime.Tracking?.FinishDate;

    [ObservableProperty]
    public partial bool IsMovie { get; set; } = anime.MediaFormat == AnimeMediaFormat.Movie;

    public async Task InitializeAsync()
    {
        Anime = await _metadataService.GetAnimeAsync(Anime.Id) ?? Anime;

        this.WhenAnyValue(x => x.Status, x => x.Progress, x => x.Score, x => x.StartDate, x => x.FinishDate)
            .Skip(1)
            .Select(x => new Tracking
            {
                Status = x.Item1,
                WatchedEpisodes = x.Item2,
                Score = x.Item3,
                StartDate = x.Item4,
                FinishDate = x.Item5
            })
            .SelectMany(tracking => trackingUpdater.UpdateTracking(Anime, tracking).ToObservable())
            .Subscribe();
    }

    [ObservableProperty] public partial INavigator? Navigator { get; set; }
}
