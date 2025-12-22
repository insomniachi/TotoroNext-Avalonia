using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface ITrackingService
{
    Guid Id { get; }
    public string Name { get; }
    Task<Tracking> Update(long id, Tracking tracking);
    Task<bool> Remove(long id);
    Task<List<Models.AnimeModel>> GetUserList(CancellationToken ct);
}