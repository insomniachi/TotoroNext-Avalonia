namespace TotoroNext.Anime.Abstractions;

public interface IAnimeMappingService
{
    AnimeId? GetId(Models.AnimeModel anime);
}