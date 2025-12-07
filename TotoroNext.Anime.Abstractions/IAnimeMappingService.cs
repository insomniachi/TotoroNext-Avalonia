namespace TotoroNext.Anime.Abstractions;

public interface IAnimeMappingService
{
    AnimeId? GetId(AnimeModel anime);
}