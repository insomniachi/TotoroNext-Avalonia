using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;

namespace TotoroNext.Anime.Local;

public class TrackingService : ITrackingService
{
    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public Task<Tracking> Update(long id, Tracking tracking)
    {
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalTracking>();
        collection.Upsert(new LocalTracking { Id = id, Tracking = tracking });
        return Task.FromResult(tracking);
    }

    public Task<bool> Remove(long id)
    {
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalTracking>();
        return Task.FromResult(collection.Delete(id));
    }

    public async Task<List<AnimeModel>> GetUserList()
    {
        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
            var tracking = db.GetCollection<LocalTracking>();
            var anime = db.GetCollection<LocalAnimeModel>();
            var trackedIds = tracking.FindAll().Select(t => t.Id).ToHashSet();
            var list = anime.Find(a => trackedIds.Contains(a.AnilistId))
                            .Select(x => LocalModelConverter.ToAnimeModel(x, anime))
                            .ToList();
            tcs.SetResult(list);
        });

        return await tcs.Task;
    }
}