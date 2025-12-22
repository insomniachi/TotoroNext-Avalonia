namespace TotoroNext.Anime.Abstractions;

public interface ILocalTrackingService : ITrackingService
{
    void SyncList(List<Models.AnimeModel> animeList);
    Task ExportList(List<Models.AnimeModel> animeList);
    Task<List<Models.AnimeModel>> GetPrequelsAndSequelsWithoutTracking(List<Models.AnimeModel> animeList, CancellationToken ct);
}

public interface ILocalMetadataService : IMetadataService
{
    Task<Models.AnimeModel> GetAnimeWithoutAdditionalInfoAsync(long id);
}