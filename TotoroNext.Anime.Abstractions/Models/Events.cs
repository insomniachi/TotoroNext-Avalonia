namespace TotoroNext.Anime.Abstractions.Models;

public class PlaybackState
{
    public required AnimeModel Anime { get; init; }
    public required Episode Episode { get; init; }
    public required TimeSpan Duration { get; init; }
    public required TimeSpan Position { get; init; }
}

public class SongPlaybackState
{
    public required AnimeModel Anime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required TimeSpan Position { get; init; }
    public required AnimeTheme Song { get; init; }
}

public class PlaybackEnded;

public class TrackingUpdated
{
    public required AnimeModel Anime { get; init; }
    public required Episode Episode { get; init; }
}

public class DownloadRequest
{
    public required AnimeModel Anime { get; init; }
    public required IAnimeProvider Provider { get; init; }
    public required SearchResult SearchResult { get; init; }
    public required int EpisodeStart { get; init; }
    public required int EpisodeEnd { get; init; }
    public string? SaveFolder { get; init; }
    public string? FilenameFormat { get; init; }
}