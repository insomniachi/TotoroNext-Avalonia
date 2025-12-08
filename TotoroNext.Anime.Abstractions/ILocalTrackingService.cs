namespace TotoroNext.Anime.Abstractions;

public interface ILocalTrackingService : ITrackingService
{
    void SyncList(List<AnimeModel> animeList);
    Task ExportList(List<AnimeModel> animeList);
}