using System.Reactive;
using System.Reactive.Linq;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using ReactiveMarbles.ObservableEvents;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Abstractions;

public class EmbeddedVlcMediaPlayer : IEmbeddedVlcMediaPlayer
{
    private static readonly LibVLC LibVlc = new();

    public EmbeddedVlcMediaPlayer()
    {
        StateChanged = Observable.Merge(MediaPlayer.Events().Playing,
                                        MediaPlayer.Events().Paused,
                                        MediaPlayer.Events().EndReached,
                                        MediaPlayer.Events().Stopped).Select(_ => ConvertState(MediaPlayer.State));
    }

    public MediaPlayer MediaPlayer { get; } = new(LibVlc);
    public IObservable<MediaPlayerState> StateChanged { get; }
    public IObservable<TimeSpan> DurationChanged => MediaPlayer.Events().LengthChanged.Select(e => TimeSpan.FromMilliseconds(e.Length));
    public IObservable<TimeSpan> PositionChanged => MediaPlayer.Events().TimeChanged.Select(e => TimeSpan.FromMilliseconds(e.Time));
    public IObservable<Unit> PlaybackStopped => MediaPlayer.Events().Stopped.Select(_ => Unit.Default);

    public MediaPlayerState CurrentState => ConvertState(MediaPlayer.State);

    public void Play(Media media, TimeSpan startPosition)
    {
        var vlcMedia = new LibVLCSharp.Shared.Media(LibVlc, media.Uri);

        if (media.Metadata.Headers?.TryGetValue("user-agent", out var userAgent) == true)
        {
            vlcMedia.AddOption($":http-user-agent={userAgent}");
        }

        if (media.Metadata.Headers?.TryGetValue("referer", out var referer) == true)
        {
            vlcMedia.AddOption($":http-referrer={referer}");
        }

        MediaPlayer.Play(vlcMedia);
    }

    public Task SeekTo(TimeSpan position)
    {
        MediaPlayer.SeekTo(position);
        return Task.CompletedTask;
    }

    public void Pause()
    {
        MediaPlayer.Pause();
    }

    public void Play()
    {
        MediaPlayer.Play();
    }

    public void Stop()
    {
        MediaPlayer.Stop();
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

internal class BackgroundInitializer(IServiceScopeFactory serviceScopeFactory) : IBackgroundInitializer
{
    public Task BackgroundInitializeAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        try
        {
            scope.ServiceProvider.GetService<IEmbeddedVlcMediaPlayer>(); // Force VLC initialization
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return FfBinaries.EnsureExists();
    }
}