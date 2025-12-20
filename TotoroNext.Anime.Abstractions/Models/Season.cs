namespace TotoroNext.Anime.Abstractions.Models;

public sealed record Season(AnimeSeason SeasonName, int Year)
{
    public Season() : this(AnimeSeason.Winter, 1900)
    {
        
    }
}