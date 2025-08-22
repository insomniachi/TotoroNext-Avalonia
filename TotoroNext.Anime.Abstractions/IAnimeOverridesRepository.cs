using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeOverridesRepository
{
    AnimeOverrides? GetOverrides(long id);
    void CreateOrUpdate(long id, AnimeOverrides overrides);
    void Revert(long id);
    bool Remove(long id);
}