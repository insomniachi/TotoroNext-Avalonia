using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface ILocalTrackingService : ITrackingService
{
    void SyncList(List<AnimeModel> animeList);
    Task ExportList(List<AnimeModel> animeList);
    Task<List<AnimeModel>> GetPrequelsAndSequelsWithoutTracking(List<AnimeModel> animeList, CancellationToken ct);
}

public interface ILocalMetadataService : IMetadataService
{
    Task<List<AnimeModel>> BuilderRelationshipsAsync(long id, CancellationToken ct);

    Task Edit(AnimeModel id);
}