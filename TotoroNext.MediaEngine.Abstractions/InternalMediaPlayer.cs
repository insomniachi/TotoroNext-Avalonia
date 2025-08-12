using System.Reactive;
using System.Reactive.Linq;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using ReactiveMarbles.ObservableEvents;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Abstractions;

public class InternalMediaPlayer : IInternalMediaPlayer
{
    private static readonly LibVLC LibVlc = new();
    private readonly MediaPlayer _mediaPlayer = new(LibVlc);

    public InternalMediaPlayer()
    {
        StateChanged = Observable.Merge(_mediaPlayer.Events().Playing,
                                        _mediaPlayer.Events().Paused,
                                        _mediaPlayer.Events().EndReached,
                                        _mediaPlayer.Events().Stopped).Select(_ => ConvertState(_mediaPlayer.State));
    }

    public IObservable<MediaPlayerState> StateChanged { get; }
    public IObservable<TimeSpan> DurationChanged => _mediaPlayer.Events().LengthChanged.Select(e => TimeSpan.FromMilliseconds(e.Length));
    public IObservable<TimeSpan> PositionChanged => _mediaPlayer.Events().TimeChanged.Select(e => TimeSpan.FromMilliseconds(e.Time));
    public IObservable<Unit> PlaybackStopped => _mediaPlayer.Events().Stopped.Select(_ => Unit.Default);

    public MediaPlayerState CurrentState => ConvertState(_mediaPlayer.State);

    public void Play(Media media, TimeSpan startPosition)
    {
        var vlcMedia = new LibVLCSharp.Shared.Media(LibVlc, media.Uri);
        _mediaPlayer.Play(vlcMedia);
    }

    public Task SeekTo(TimeSpan position)
    {
        _mediaPlayer.SeekTo(position);
        return Task.CompletedTask;
    }

    public void Pause()
    {
        _mediaPlayer.Pause();
    }

    public void Play()
    {
        _mediaPlayer.Play();
    }

    public void Stop()
    {
        _mediaPlayer.Stop();
    }

    private static MediaPlayerState ConvertState(VLCState state)
    {
        return state switch
        {
            VLCState.Opening => MediaPlayerState.Opening,
            VLCState.Playing => MediaPlayerState.Playing,
            VLCState.Paused => MediaPlayerState.Paused,
            VLCState.Stopped => MediaPlayerState.Stopped,
            VLCState.Ended => MediaPlayerState.Ended,
            _ => MediaPlayerState.Error
        };
    }
}

internal class Initializer(IServiceScopeFactory serviceScopeFactory) : IInitializer
{
    public Task InitializeAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        scope.ServiceProvider.GetService<IInternalMediaPlayer>(); // Force VLC initialization
        return Task.CompletedTask;
    }
}