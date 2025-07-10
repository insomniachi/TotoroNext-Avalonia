namespace TotoroNext.Anime.Abstractions.Models;

public class PlaybackState
{
    public required AnimeModel Anime { get; init; }
    public required Episode Episode { get; init; }
    public required TimeSpan Duration { get; init; }
    public required TimeSpan Position { get; init; }
}

public class PlaybackEnded;

public class TrackingUpdated
{
    public required AnimeModel Anime { get; init; }
    public required Episode Episode { get; init; }
}

