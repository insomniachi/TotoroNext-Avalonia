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
    IAnimeRelations relations,
    IDialogService dialogService,
    IMessenger messenger,
    ILocalSettingsService localSettingsService) : ObservableObject,
                                                  IAsyncInitializable,
                                                  IDisposable,
                                                  IKeyBindingsProvider
{
    private TimeSpan _currentPosition;
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

    [ObservableProperty] public partial bool IsMovie { get; set; }

    [ObservableProperty] public partial bool IsFullscreen { get; set; }

    [ObservableProperty] public partial bool IsEpisodesVisible { get; set; } = true;

    [ObservableProperty] public partial bool IsEpisodesLoading { get; set; } = true;

    public async Task InitializeAsync()
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
            .SelectMany(anime => anime.GetEpisodes().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(e =>
            {
                if (e.Count > (Anime.TotalEpisodes ?? 0) && relations.FindRelation(Anime!) is { } relation)
                {
                    var eps = e.Where(x => x.Number >= relation.SourceEpisodesRage.Start && x.Number <= relation.SourceEpisodesRage.End).ToList();
                    foreach (var ep in eps)
                    {
                        ep.Number -= relation.SourceEpisodesRage.Start - 1;
                    }

                    Episodes = eps;
                }
                else
                {
                    Episodes = e;
                }
            });


        IsEpisodesLoading = true;
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

                IsEpisodesLoading = false;
            });

        if (continueWatching)
        {
            this.WhenAnyValue(x => x.Episodes)
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(eps =>
                {
                    if (IsMovie)
                    {
                        SelectedEpisode = eps.FirstOrDefault();
                    }
                    else
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
                    }
                });
        }

        this.WhenAnyValue(x => x.SelectedEpisode)
            .Do(_ => Servers = [])
            .WhereNotNull()
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
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(x => Play(x).ToObservable())
            .Subscribe();

        this.WhenAnyValue(x => x.CurrentSegment)
            .WhereNotNull()
            .Where(x => x is { Type : MediaSectionType.Opening or MediaSectionType.Ending })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(OnPlayingOpeningOrEnding)
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
        InitializeListeners();
    }

    public void Dispose()
    {
        messenger.Send(new PlaybackEnded { Id = SelectedEpisode?.Id ?? "" });
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

    private async Task<(MediaSegment Segment, MessageBoxResult Result)> OnPlayingOpeningOrEnding(MediaSegment segment)
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
            SkipMethod.Always => new ValueTuple<MediaSegment, MessageBoxResult>(segment, MessageBoxResult.Yes),
            SkipMethod.Never => new ValueTuple<MediaSegment, MessageBoxResult>(segment, MessageBoxResult.No),
            _ => new ValueTuple<MediaSegment, MessageBoxResult>(segment, await dialogService.AskSkip(segment.Type.ToString()))
        };
    }

    private void InitializePublishers()
    {
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
            .Do(_ => messenger.Send(new PlaybackEnded { Id = SelectedEpisode?.Id ?? "" }))
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
        if (SelectedEpisode is null)
        {
            return;
        }

        IEnumerable<string?> parts = Anime?.MediaFormat is AnimeMediaFormat.Movie
            ? [ProviderResult?.Title]
            : [ProviderResult?.Title, $"Episode {SelectedEpisode.Number}", source.Title ?? SelectedEpisode.Info?.Titles.English];

        var title = string.Join(" - ", parts.Where(x => !string.IsNullOrEmpty(x)));
        var segments = await GetMediaSegments(source, SelectedEpisode);
        var metadata = new MediaMetadata(title, source.Headers, segments, source.Subtitle);
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

        var answer = await dialogService.Question("Tracking Updated", "Play the next episode?");
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

        if (segments.Count >= 2 ||
            Anime is not { ExternalIds.MyAnimeList: not null } ||
            segmentsFactory.CreateDefault() is not { } segmentsProvider)
        {
            return [.. segments.MakeContiguousSegments(_duration)];
        }

        var providerSegments = await segmentsProvider.GetSegments(Anime.ExternalIds.MyAnimeList.Value, episode.Number, _duration.TotalSeconds);
        var extras = providerSegments.Where(x => segments.All(s => s.Type != x.Type));
        segments.AddRange(extras);

        return [.. segments.MakeContiguousSegments(_duration)];
    }

    public static bool IsMagnetLink(Uri uri)
    {
        return uri.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase);
    }
}