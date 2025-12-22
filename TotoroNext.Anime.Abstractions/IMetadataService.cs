using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IMetadataService
{
    Guid Id { get; }
    public string Name { get; }
    Task<Models.AnimeModel> GetAnimeAsync(long id);
    Task<List<Models.AnimeModel>> SearchAnimeAsync(string term);
    Task<List<Models.AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request);
    Task<List<EpisodeInfo>> GetEpisodesAsync(Models.AnimeModel anime);
    Task<List<CharacterModel>> GetCharactersAsync(long animeId);
    Task<List<string>> GetGenresAsync();
    Task<List<Models.AnimeModel>> GetPopularAnimeAsync(CancellationToken ct);
    Task<List<Models.AnimeModel>> GetUpcomingAnimeAsync(CancellationToken ct);
    Task<List<Models.AnimeModel>> GetAiringToday(CancellationToken ct);
}