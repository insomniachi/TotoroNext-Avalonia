using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeExtensionService
{
    AnimeOverrides? GetExtension(long id);
    void CreateOrUpdateExtension(long id, AnimeOverrides overrides);
    void RemoveExtension(long id);
    bool IsInIncognitoMode(long id);
    Task<SearchResult?> SearchOrSelectAsync(Models.AnimeModel anime);
    Task<SearchResult?> SearchAsync(Models.AnimeModel anime);
}