namespace TotoroNext.Anime.Abstractions;

public interface ITrackingService
{
    string ServiceName { get; }
    Task<Tracking> Update(long id, Tracking tracking);
    Task<bool> Remove(long id);
    Task<List<AnimeModel>> GetUserList();
}
