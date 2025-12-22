using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeDetailsViewModel(
    AnimeModel anime,
    ITrackingUpdater trackingUpdater,
    IFactory<IMetadataService, Guid> metadataServiceFactory) : DialogViewModel, IAsyncInitializable, INavigatorHost
{
    [ObservableProperty] public partial AnimeModel Anime { get; set; } = anime;

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = anime.Tracking?.Status;

    [ObservableProperty] public partial int? Progress { get; set; } = anime.Tracking?.WatchedEpisodes;

    [ObservableProperty] public partial int? Score { get; set; } = anime.Tracking?.Score;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; } = anime.Tracking?.StartDate == new DateTime() ? null : anime.Tracking?.StartDate;

    [ObservableProperty]
    public partial DateTime? FinishDate { get; set; } = anime.Tracking?.FinishDate == new DateTime() ? null : anime.Tracking?.FinishDate;

    public bool IsEpisodesTabVisible { get; } = anime.MediaFormat != AnimeMediaFormat.Movie && anime.AiringStatus != AiringStatus.NotYetAired;
    public bool HasAnimeAired { get; } = anime.AiringStatus is not AiringStatus.NotYetAired;
    public bool HasTrailers => Anime.Trailers is { Count: > 0 };
    public bool HasRelated => Anime.Related.Any();
    public bool HasRecommended => Anime.Recommended.Any();
    public bool IsTracked => Anime.Tracking is not null;

    public async Task InitializeAsync()
    {
        var service = metadataServiceFactory.CreateDefault();
        if (service is null)
        {
            return;
        }

        this.WhenAnyValue(x => x.Anime.Tracking)
            .WhereNotNull()
            .Subscribe(_ => { OnPropertyChanged(nameof(IsTracked)); });

        this.WhenAnyValue(x => x.Anime)
            .WhereNotNull()
            .Subscribe(_ =>
            {
                OnPropertyChanged(nameof(HasRelated));
                OnPropertyChanged(nameof(HasRecommended));
                OnPropertyChanged(nameof(HasTrailers));
            });

        if (await service.GetAnimeAsync(Anime.Id) is { } model)
        {
            Anime = model;
        }

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