using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeExtensionService
{
    AnimeOverrides? GetExtension(long id);
    void CreateOrUpdateExtension(long id, AnimeOverrides overrides);
    void RemoveExtension(long id);
    bool IsInIncognitoMode(long id);
    string GetSelectedResult(AnimeModel anime);
    IAnimeProvider GetProvider(long id);
    Task<SearchResult?> SearchAndSelectAsync(AnimeModel anime);
    Task<SearchResult?> SearchAsync(AnimeModel anime);
}