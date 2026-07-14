namespace TotoroNext.Anime.Abstractions;

public interface IAnimeMappingService
{
    Task<AnimeId?> GetId(Models.AnimeModel anime);
}