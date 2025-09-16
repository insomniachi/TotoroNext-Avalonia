using System.Reactive;
using LibVLCSharp.Shared;

namespace TotoroNext.MediaEngine.Abstractions;

public interface IMediaPlayer
{
    IObservable<TimeSpan> DurationChanged { get; }
    IObservable<TimeSpan> PositionChanged { get; }
    IObservable<Unit> PlaybackStopped { get; }
    void Play(Media media, TimeSpan startPosition);
}

public interface ISeekable
{
    Task SeekTo(TimeSpan position);
}

public interface IEmbeddedVlcMediaPlayer : IMediaPlayer, ISeekable
{
    MediaPlayer MediaPlayer { get; }
    MediaPlayerState CurrentState { get; }
    IObservable<MediaPlayerState> StateChanged { get; }
    void Pause();
    void Play();
}

public record Media(Uri Uri, MediaMetadata Metadata);

public enum MediaSectionType
{
    Recap,
    Opening,
    Content,
    Ending,
    Preview
}

public enum MediaPlayerState
{
    Opening,
    Playing,
    Paused,
    Stopped,
    Ended,
    Error
}

public record MediaSegment(MediaSectionType Type, TimeSpan Start, TimeSpan End);

public record MediaMetadata(
    string Title,
    IDictionary<string, string>? Headers = null,
    IReadOnlyList<MediaSegment>? MedaSections = null,
    string? Subtitle = null);