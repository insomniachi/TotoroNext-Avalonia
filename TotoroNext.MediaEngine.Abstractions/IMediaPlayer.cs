using System.Reactive;

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

public interface IInternalMediaPlayer : IMediaPlayer, ISeekable
{
    void Pause();
    void Play();
    void Stop();
    MediaPlayerState CurrentState { get; }
    IObservable<MediaPlayerState> StateChanged { get; }
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
    IReadOnlyList<MediaSegment>? MedaSections = null);