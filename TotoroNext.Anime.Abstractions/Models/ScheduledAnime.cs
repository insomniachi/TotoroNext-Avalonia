namespace TotoroNext.Anime.Abstractions.Models;

public class ScheduledAnime(Models.AnimeModel anime)
{
    public DateTime Start { get; init; }
    public Models.AnimeModel Anime { get; } = anime;
}