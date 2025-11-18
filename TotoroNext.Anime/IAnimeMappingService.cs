using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime;

public interface IAnimeMappingService
{
    Task Update();
    AnimeId? GetId(AnimeModel anime);
}