namespace TotoroNext.Anime.Abstractions;

public interface IAnimeMappingService
{
    void Update(Stream dbStream);
    AnimeId? GetId(AnimeModel anime);
}