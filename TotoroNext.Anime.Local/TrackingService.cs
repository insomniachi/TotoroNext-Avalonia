using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Local;

internal class TrackingService(ILiteDbContext dbContext) : ITrackingService
{
    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public Task<Tracking> Update(long id, Tracking tracking)
    {
        dbContext.Tracking.Upsert(new LocalTracking { Id = id, Tracking = tracking });
        return Task.FromResult(tracking);
    }

    public Task<bool> Remove(long id)
    {
        return Task.FromResult(dbContext.Tracking.Delete(id));
    }

    public async Task<List<AnimeModel>> GetUserList()
    {
        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            var trackedIds = dbContext.Tracking.FindAll().Select(t => t.Id).ToHashSet();
            var list = dbContext.Anime.Find(a => trackedIds.Contains(a.AnilistId))
                                .Select(x => LocalModelConverter.ToAnimeModel(x, dbContext.Anime))
                                .ToList();
            tcs.SetResult(list);
        });

        return await tcs.Task;
    }
}