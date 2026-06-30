using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.MediaEngine.Abstractions;

namespace TotoroNext.Anime;

public sealed class TrackingMediaPlayerContext(
    IMediaPlayer? mediaPlayer,
    IMessenger messenger) : IDisposable
{
    private readonly CompositeDisposable _disposable = new();

    public IMediaPlayer? MediaPlayer => mediaPlayer;
    public AnimeModel? Anime { get; set; }
    public Episode? SelectedEpisode { get; set; }
    public TimeSpan Position { get; set; } = TimeSpan.Zero;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public void Dispose()
    {
        _disposable.Dispose();
    }

    public void Play(Media media, TimeSpan startPosition)
    {
        MediaPlayer?.Play(media, startPosition);
    }

    public void Initialize()
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
                           Position = Position,
                           Duration = Duration,
                           IsPaused = state is MediaPlayerState.Paused
                       });
                   })
                   .DisposeWith(_disposable);

        MediaPlayer
            .PositionChanged
            .Where(_ => Anime is not null && SelectedEpisode is not null)
            .Subscribe(position =>
            {
                Position = position;
                messenger.Send(new PlaybackState
                {
                    Anime = Anime!,
                    Episode = SelectedEpisode!,
                    Position = position,
                    Duration = Duration
                });
            })
            .DisposeWith(_disposable);

        MediaPlayer
            .DurationChanged
            .Subscribe(duration => Duration = duration)
            .DisposeWith(_disposable);

        MediaPlayer
            .PlaybackStopped
            .Subscribe(_ =>
            {
                messenger.Send(new PlaybackEnded { Id = SelectedEpisode?.Id ?? "" });
                Position = TimeSpan.Zero;
                Duration = TimeSpan.Zero;
            })
            .DisposeWith(_disposable);
    }
}