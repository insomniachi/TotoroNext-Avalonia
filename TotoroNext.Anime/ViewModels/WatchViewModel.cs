using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Extensions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class WatchViewModel(
    WatchViewModelNavigationParameter navigationParameter,
    IFactory<IMediaPlayer, Guid> mediaPlayerFactory,
    IFactory<IMediaSegmentsProvider, Guid> segmentsFactory,
    IPlaybackProgressService progressService,
    IAnimeOverridesRepository animeOverridesRepository,
    IDialogService dialogService,
    IMessenger messenger) : ObservableObject, IAsyncInitializable, IDisposable
{
    private TimeSpan _duration;
    private Media? _media;

    public IMediaPlayer MediaPlayer { get; } = mediaPlayerFactory.CreateDefault();

    [ObservableProperty] public partial SearchResult? ProviderResult { get; set; }

    [ObservableProperty] public partial AnimeModel? Anime { get; set; }

    [ObservableProperty] public partial Episode? SelectedEpisode { get; set; }

    [ObservableProperty] public partial VideoServer? SelectedServer { get; set; }

    [ObservableProperty] public partial VideoSource? SelectedSource { get; set; }

    [ObservableProperty] public partial List<Episode>? Episodes { get; set; } = [];

    [ObservableProperty] public partial List<VideoServer> Servers { get; set; } = [];

    [ObservableProperty] public partial List<VideoSource> Sources { get; set; } = [];

    [ObservableProperty] public partial MediaSegment? CurrentSegment { get; set; }

    public void Dispose()
    {
        messenger.Send(new PlaybackEnded());
        if (Anime?.Id is { } id)
        {
            animeOverridesRepository.Revert(id);
        }
    }

    public async Task InitializeAsync()
    {
        (ProviderResult, Anime, Episodes, SelectedEpisode, var continueWatching) = navigationParameter;

        if (Anime is not null)
        {
            var infos = await Anime.GetEpisodes();
            
            this.WhenAnyValue(x => x.Episodes)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(eps =>
                {
                    foreach (var ep in eps)
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        ep.Info = infos.FirstOrDefault(x => x.EpisodeNumber == ep.Number);
                    }
                });
        }
        
        
        this.WhenAnyValue(x => x.ProviderResult)
            .WhereNotNull()
            .Where(_ => Episodes is { Count: 0 } or null)
            .SelectMany(anime => anime.GetEpisodes().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(e => Episodes = e);

        if (continueWatching)
        {
            this.WhenAnyValue(x => x.Episodes)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(eps =>
                {
                    var nextUp = (Anime?.Tracking?.WatchedEpisodes ?? 0) + 1;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (eps.FirstOrDefault(x => x.Number == nextUp) is not { } nextEp)
                    {
                        return;
                    }

                    if (Anime?.Id is { } id)
                    {
                        var progress = progressService.GetProgress(id);
                        if (progress.TryGetValue(nextUp, out var epProgress))
                        {
                            nextEp.StartPosition = TimeSpan.FromSeconds(epProgress.Position);
                        }
                    }

                    SelectedEpisode = nextEp;
                });
        }

        this.WhenAnyValue(x => x.SelectedEpisode)
            .WhereNotNull()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(ep => ep.GetServersAsync().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(servers => Servers = servers);

        this.WhenAnyValue(x => x.Servers)
            .Where(x => x is { Count: > 0 })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => SelectedServer = x.First());

        this.WhenAnyValue(x => x.SelectedServer)
            .WhereNotNull()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(server => server.Extract().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(sources => Sources = sources);

        this.WhenAnyValue(x => x.Sources)
            .Where(x => x is { Count: 1 })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => SelectedSource = x.First());

        this.WhenAnyValue(x => x.SelectedSource)
            .WhereNotNull()
            .SelectMany(x => Play(x).ToObservable())
            .Subscribe();

        this.WhenAnyValue(x => x.CurrentSegment)
            .WhereNotNull()
            .Where(x => x is { Type : MediaSectionType.Opening or MediaSectionType.Ending })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(AskSkip)
            .SelectMany(x =>
            {
                if (MediaPlayer is not ISeekable seekable)
                {
                    return Task.CompletedTask.ToObservable();
                }

                return x.Result is MessageBoxResult.Yes
                    ? seekable.SeekTo(x.Segment.End).ToObservable()
                    : Task.CompletedTask.ToObservable();
            })
            .Subscribe();

        InitializePublishers();
    }

    private async Task<(MediaSegment Segment, MessageBoxResult Result)> AskSkip(MediaSegment segment)
    {
        return new ValueTuple<MediaSegment, MessageBoxResult>(segment, await dialogService.AskSkip());
    }

    private void InitializePublishers()
    {
        MediaPlayer
            .PositionChanged
            .Where(_ => Anime is not null && SelectedEpisode is not null)
            .Subscribe(position => messenger.Send(new PlaybackState
            {
                Anime = Anime!,
                Episode = SelectedEpisode!,
                Position = position,
                Duration = _duration
            }));

        MediaPlayer
            .DurationChanged
            .Subscribe(duration => _duration = duration);

        MediaPlayer
            .PlaybackStopped
            .Do(_ => messenger.Send(new PlaybackEnded()))
            .Where(_ => SelectedEpisode is { IsCompleted: true } && Episodes is not null)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(_ => AskIfContinueWatching())
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(nexEp => SelectedEpisode = nexEp);

        if (MediaPlayer is ISeekable)
        {
            MediaPlayer
                .PositionChanged
                .Where(_ => _media is not null)
                .Select(position => (_media!.Metadata.MedaSections ?? []).FirstOrDefault(item => position > item.Start && position < item.End) )
                .WhereNotNull()
                .DistinctUntilChanged()
                .Subscribe(segment => CurrentSegment = segment);
        }
    }

    private async Task Play(VideoSource source)
    {
        if (SelectedEpisode is null)
        {
            return;
        }

        IEnumerable<string?> parts = [ProviderResult?.Title, $"Episode {SelectedEpisode.Number}", source.Title];
        var title = string.Join(" - ", parts.Where(x => !string.IsNullOrEmpty(x)));

        _duration = MediaHelper.GetDuration(source.Url, source.Headers);
        List<MediaSegment> segments = [];

        if (Anime is { ExternalIds.MyAnimeList: not null } && segmentsFactory.CreateDefault() is { } segmentsProvider)
        {
            segments.AddRange(await segmentsProvider.GetSegments(Anime.ExternalIds.MyAnimeList.Value, SelectedEpisode.Number,
                                                                 _duration.TotalSeconds));
        }

        var metadata = new MediaMetadata(title, source.Headers, segments);
        _media = new Media(source.Url, metadata);

        MediaPlayer.Play(_media, SelectedEpisode.StartPosition);
    }

    private async Task<Episode?> AskIfContinueWatching()
    {
        if (SelectedEpisode is null || Episodes is null)
        {
            return null;
        }

        if (Episodes.FirstOrDefault(x => x.Number > SelectedEpisode.Number) is not { } nextEp)
        {
            return null;
        }

        var question = $"Completed watching {Anime?.Title} - Episode {SelectedEpisode.Number}, play the next episode ?";
        var answer = await dialogService.Question("Tracking Updated", question);
        return answer is MessageBoxResult.Yes
            ? nextEp
            : null;
    }
}