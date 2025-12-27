using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
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
    IAnimeExtensionService animeExtensionService,
    IAnimeMappingService animeMappingService,
    IAnimeRelations relations,
    IDialogService dialogService,
    IMessenger messenger,
    ILocalSettingsService localSettingsService) : ObservableObject,
                                                  IInitializable,
                                                  IDisposable,
                                                  IKeyBindingsProvider
{
    private TimeSpan _currentPosition;
    private TimeSpan _duration;
    private bool _isCancelled;
    private Media? _media;

    public IMediaPlayer? MediaPlayer { get; } = mediaPlayerFactory.CreateDefault();

    [ObservableProperty] public partial SearchResult? ProviderResult { get; set; }

    [ObservableProperty] public partial AnimeModel? Anime { get; set; }

    [ObservableProperty] public partial Episode? SelectedEpisode { get; set; }

    [ObservableProperty] public partial VideoServer? SelectedServer { get; set; }

    [ObservableProperty] public partial VideoSource? SelectedSource { get; set; }

    [ObservableProperty] public partial List<Episode>? Episodes { get; set; } = [];

    [ObservableProperty] public partial List<VideoServer> Servers { get; set; } = [];

    [ObservableProperty] public partial List<VideoSource> Sources { get; set; } = [];

    [ObservableProperty] public partial MediaSegment? CurrentSegment { get; set; }

    [ObservableProperty] public partial bool IsMovie { get; set; }

    [ObservableProperty] public partial bool IsFullscreen { get; set; }

    [ObservableProperty] public partial bool IsEpisodesVisible { get; set; } = true;

    [ObservableProperty] public partial bool IsEpisodesLoading { get; set; } = true;

    [ObservableProperty] public partial bool AutoPlayNextEpisode { get; set; }
    
    [ObservableProperty] public partial bool IsFetchingStream { get; set; }

    public void Dispose()
    {
        messenger.Send(new PlaybackEnded { Id = SelectedEpisode?.Id ?? "" });
        _isCancelled = true;
    }

    public void Initialize()
    {
        (ProviderResult, Anime, Episodes, SelectedEpisode, var continueWatching) = navigationParameter;

        if (Anime is null)
        {
            return;
        }

        this.WhenAnyValue(x => x.Anime)
            .WhereNotNull()
            .Select(x => x is { MediaFormat: AnimeMediaFormat.Movie })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isMovie => IsMovie = isMovie);

        this.WhenAnyValue(x => x.ProviderResult)
            .WhereNotNull()
            .Where(_ => Episodes is { Count: 0 } or null)
            .ObserveOn(RxApp.MainThreadScheduler).Do(_ => IsEpisodesLoading = true)
            .Select(providerResult => Observable.FromAsync(ct => GetEpisodesAndMetadata(Anime, providerResult, ct)))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateEpisodeMetadata);

        if (continueWatching)
        {
            this.WhenAnyValue(x => x.Episodes)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SelectNextEpisode);
        }

        this.WhenAnyValue(x => x.SelectedEpisode)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ =>
            {
                Servers = [];
                IsFetchingStream = true;
            })
            .Select(ep => Observable.FromAsync(ep.GetServers))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(servers => Servers = servers);

        this.WhenAnyValue(x => x.Servers)
            .Where(x => x is { Count: > 0 })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => SelectedServer = x.First());

        this.WhenAnyValue(x => x.SelectedServer)
            .WhereNotNull()
            .DistinctUntilChanged()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(server => Observable.FromAsync(server.GetSources))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(sources => Sources = sources);

        this.WhenAnyValue(x => x.Sources)
            .Where(x => x is { Count: 1 })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => SelectedSource = x.First());

        this.WhenAnyValue(x => x.SelectedSource)
            .WhereNotNull()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(x => Play(x).ToObservable())
            .Subscribe();

        this.WhenAnyValue(x => x.CurrentSegment)
            .WhereNotNull()
            .Where(x => x is { Type : MediaSectionType.Opening or MediaSectionType.Ending })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(ShouldSkipMediaSegment)
            .SelectMany(x => HandleMediaSegment(x).ToObservable())
            .Subscribe();

        InitializePublishers();
        InitializeListeners();
    }

    public IEnumerable<KeyBinding> GetKeyBindings()
    {
        if (MediaPlayer is not IEmbeddedVlcMediaPlayer embeddedPlayer)
        {
            yield break;
        }

        yield return new KeyBinding
        {
            Gesture = new KeyGesture(Key.F11),
            Command = new RelayCommand(() => messenger.Send<EnterFullScreen>())
        };
        yield return new KeyBinding
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new RelayCommand(() => messenger.Send<ExitFullScreen>())
        };
        yield return new KeyBinding
        {
            Gesture = new KeyGesture(Key.Space),
            Command = new RelayCommand(() =>
            {
                switch (embeddedPlayer.CurrentState)
                {
                    case MediaPlayerState.Playing:
                        embeddedPlayer.Pause();
                        break;
                    case MediaPlayerState.Paused:
                        embeddedPlayer.Play();
                        break;
                    case MediaPlayerState.Opening:
                    case MediaPlayerState.Stopped:
                    case MediaPlayerState.Ended:
                    case MediaPlayerState.Error:
                    default:
                        break;
                }
            })
        };
        yield return new KeyBinding
        {
            Gesture = new KeyGesture(Key.Right),
            Command = new RelayCommand(() => embeddedPlayer.SeekTo(_currentPosition + TimeSpan.FromSeconds(10)))
        };
        yield return new KeyBinding
        {
            Gesture = new KeyGesture(Key.Left),
            Command = new RelayCommand(() => embeddedPlayer.SeekTo(_currentPosition - TimeSpan.FromSeconds(10)))
        };
    }

    private async Task<(MediaSegment Segment, MessageBoxResult Result)> ShouldSkipMediaSegment(MediaSegment segment)
    {
        if (Anime is null)
        {
            return new ValueTuple<MediaSegment, MessageBoxResult>(segment, await dialogService.AskSkip(segment.Type.ToString()));
        }

        var @override = animeExtensionService.GetExtension(Anime.Id);
        var method = segment.Type switch
        {
            MediaSectionType.Opening => @override?.OpeningSkipMethod ??
                                        localSettingsService.ReadSetting<SkipMethod>(nameof(@override.OpeningSkipMethod)),
            MediaSectionType.Ending => @override?.EndingSkipMethod ??
                                       localSettingsService.ReadSetting<SkipMethod>(nameof(@override.EndingSkipMethod)),
            _ => SkipMethod.Ask
        };

        return method switch
        {
            SkipMethod.Always => new ValueTuple<MediaSegment, MessageBoxResult>(segment,
                                                                                await dialogService.AskSkip(segment.Type.ToString(),
                                                                                 MessageBoxResult.Yes)),
            SkipMethod.Never => new ValueTuple<MediaSegment, MessageBoxResult>(segment, MessageBoxResult.No),
            _ => new ValueTuple<MediaSegment, MessageBoxResult>(segment, await dialogService.AskSkip(segment.Type.ToString()))
        };
    }

    private void InitializePublishers()
    {
        if (MediaPlayer is null)
        {
            return;
        }

        MediaPlayer.StateChanged
                   .Subscribe(state =>
                   {
                       messenger.Send(new PlaybackState
                       {
                           Anime = Anime!,
                           Episode = SelectedEpisode!,
                           Position = _currentPosition,
                           Duration = _duration,
                           IsPaused = state is MediaPlayerState.Paused
                       });
                   });

        MediaPlayer
            .PositionChanged
            .Where(_ => Anime is not null && SelectedEpisode is not null)
            .Subscribe(position =>
            {
                _currentPosition = position;
                messenger.Send(new PlaybackState
                {
                    Anime = Anime!,
                    Episode = SelectedEpisode!,
                    Position = position,
                    Duration = _duration
                });
            });

        MediaPlayer
            .DurationChanged
            .Subscribe(duration => _duration = duration);

        MediaPlayer
            .PlaybackStopped
            .Do(_ =>
            {
                messenger.Send(new PlaybackEnded { Id = SelectedEpisode?.Id ?? "" });
                CurrentSegment = null;
                _currentPosition = TimeSpan.Zero;
                _duration = TimeSpan.Zero;
            })
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
                .Select(position => (_media!.Metadata.MedaSections ?? []).FirstOrDefault(item => position > item.Start && position < item.End))
                .WhereNotNull()
                .DistinctUntilChanged()
                .Subscribe(segment => CurrentSegment = segment);
        }
    }

    private void InitializeListeners()
    {
        messenger.Register<EnterFullScreen>(this, (_, _) => IsFullscreen = true);
        messenger.Register<ExitFullScreen>(this, (_, _) => IsFullscreen = false);

        this.WhenAnyValue(x => x.IsMovie, x => x.IsFullscreen)
            .Select(x => x is { Item1: false, Item2: false })
            .Subscribe(isVisible => IsEpisodesVisible = isVisible);
    }

    private async Task Play(VideoSource source)
    {
        if (SelectedEpisode is null || MediaPlayer is null)
        {
            return;
        }

        IEnumerable<string?> parts = Anime?.MediaFormat is AnimeMediaFormat.Movie
            ? [ProviderResult?.Title]
            : [ProviderResult?.Title, $"Episode {SelectedEpisode.Number}", source.Title ?? SelectedEpisode.Info?.Titles.English];

        var title = string.Join(" - ", parts.Where(x => !string.IsNullOrEmpty(x)));
        var segments = await GetMediaSegments(source, SelectedEpisode);

        RxApp.MainThreadScheduler.Schedule(() => IsFetchingStream = false);
        
        var metadata = new MediaMetadata(title, source.Headers, segments, source.Subtitle);
        _media = new Media(source.Url, metadata);

        if (_isCancelled)
        {
            return;
        }

        MediaPlayer.Play(_media, SelectedEpisode.StartPosition);
    }

    private async Task<Episode?> AskIfContinueWatching()
    {
        if (SelectedEpisode is null || Episodes is null || Anime is null)
        {
            return null;
        }

        if ((int)SelectedEpisode.Number == Anime.TotalEpisodes)
        {
            messenger.Send(new NavigateToKeyDialogMessage
            {
                Title = Anime.Title,
                Key = $"tracking/{Anime.ServiceName}",
                Data = Anime
            });
            return null;
        }

        if (Episodes.FirstOrDefault(x => x.Number > SelectedEpisode.Number) is not { } nextEp)
        {
            return null;
        }

        var answer = AutoPlayNextEpisode
            ? MessageBoxResult.Yes
            : await dialogService.Question("Tracking Updated", "Play the next episode?");

        return answer is MessageBoxResult.Yes
            ? nextEp
            : null;
    }

    private async ValueTask<List<MediaSegment>> GetMediaSegments(VideoSource source, Episode episode)
    {
        List<MediaSegment> segments = [];

        if (!IsMagnetLink(source.Url))
        {
            segments.AddRange(await MediaHelper.GetChapters(source.Url, source.Headers));
            _duration = MediaHelper.GetDuration(source.Url, source.Headers);
        }

        if (segments.Count >= 2)
        {
            return [.. segments.MakeContiguousSegments(_duration)];
        }

        if (source.SkipData is { } skipData)
        {
            if (skipData.Opening is { } op)
            {
                segments.Add(new MediaSegment(MediaSectionType.Opening, op.Start, op.End));
            }

            if (skipData.Ending is { } ed)
            {
                segments.Add(new MediaSegment(MediaSectionType.Ending, ed.Start, ed.End));
            }
        }

        if (Anime is null || segments.Count >= 2)
        {
            return [.. segments.MakeContiguousSegments(_duration)];
        }

        var id = animeMappingService.GetId(Anime);

        if (id is null ||
            id.MyAnimeList <= 0 ||
            segmentsFactory.CreateDefault() is not { } segmentsProvider)
        {
            return [.. segments.MakeContiguousSegments(_duration)];
        }

        var providerSegments = await segmentsProvider.GetSegments(id.MyAnimeList, episode.Number, _duration.TotalSeconds);
        var extras = providerSegments.Where(x => segments.All(s => s.Type != x.Type));
        segments.AddRange(extras);

        return [.. segments.MakeContiguousSegments(_duration)];
    }

    private void SelectNextEpisode(List<Episode> eps)
    {
        if (IsMovie)
        {
            SelectedEpisode = eps.FirstOrDefault();
        }
        else
        {
            var nextUp = (Anime?.Tracking?.WatchedEpisodes ?? 0) + 1;

            if (eps.FirstOrDefault(x => Math.Abs(x.Number - nextUp) == 0) is not { } nextEp)
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
        }
    }

    private void UpdateEpisodeMetadata(ValueTuple<List<Episode>, List<EpisodeInfo>, List<EpisodeInfo>> tuple)
    {
        var (episodes, infos, specials) = tuple;

        if (Anime is null)
        {
            return;
        }
        
        if (relations.FindRelation(Anime!) is { } relation &&
            infos.Count > 0 &&
            episodes.Count(x => x.Number > 0) != infos.Count)
        {
            var eps = episodes
                      .Where(x => x.Number >= relation.SourceEpisodesRage.Start && x.Number <= relation.SourceEpisodesRage.End)
                      .ToList();

            foreach (var ep in eps)
            {
                ep.Number -= relation.SourceEpisodesRage.Start - 1;
            }

            UpdateEpisodeInfo(eps, infos, specials);
            Episodes = eps.ToList();
        }
        else
        {
            UpdateEpisodeInfo(episodes, infos, specials);
            Episodes = episodes;
        }

        IsEpisodesLoading = false;
    }

    private static void UpdateEpisodeInfo(List<Episode> episodes, List<EpisodeInfo> infos, List<EpisodeInfo> specials)
    {
        foreach (var ep in episodes)
        {
            ep.Info = infos.FirstOrDefault(x => Math.Abs(x.AbsoluteEpisodeNumber - ep.Number) == 0);
        }

        var specialsQueue = new Queue<EpisodeInfo>(specials);
        foreach (var ep in episodes.Where(x => x.Number is <= 0))
        {
            if (!specialsQueue.TryDequeue(out var info))
            {
                continue;
            }

            ep.Info = info;
        }
    }

    private async Task HandleMediaSegment((MediaSegment Segment, MessageBoxResult Result) tuple)
    {
        var (segment, result) = tuple;

        if (MediaPlayer is not ISeekable seekable ||
            result is not MessageBoxResult.Yes)
        {
            return;
        }

        await seekable.SeekTo(segment.End);
    }

    private static async Task<(List<Episode> Episode, List<EpisodeInfo> Info, List<EpisodeInfo> Specials)> GetEpisodesAndMetadata(
        AnimeModel anime, SearchResult providerResult, CancellationToken ct)
    {
        var episodes = await providerResult.GetEpisodes(ct);
        var all = await anime.GetEpisodes(ct);
        var infos = all.Where(x => !x.IsSpecial).ToList();
        var specials = all.Where(x => x.IsSpecial).ToList();

        if (infos.Count == 0)
        {
            return new ValueTuple<List<Episode>, List<EpisodeInfo>, List<EpisodeInfo>>(episodes, infos, specials);
        }

        var min = infos.Min(x => x.AbsoluteEpisodeNumber);
        var diff = min - 1;
        if (diff > 0)
        {
            infos.ForEach(x => x.AbsoluteEpisodeNumber -= diff);
        }

        return new ValueTuple<List<Episode>, List<EpisodeInfo>, List<EpisodeInfo>>(episodes, infos, specials);
    }

    private static bool IsMagnetLink(Uri uri)
    {
        return uri.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase);
    }
}