namespace TotoroNext.Anime.Abstractions.Models;

public class ScheduledAnime(AnimeModel anime)
{
    public DateTime Start { get; init; }
    public AnimeModel Anime { get; } = anime;
}