using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Local;

internal class TrackingService(ILiteDbContext dbContext) : ILocalTrackingService
{
    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public Task<Tracking> Update(long id, Tracking tracking)
    {
        var anime = dbContext.Anime.FindById(id);
        var localTracking = new LocalTracking { Id = id, Tracking = tracking };
        anime.Tracking = localTracking;
        dbContext.Tracking.Upsert(localTracking);
        dbContext.Anime.Upsert(anime);
        return Task.FromResult(tracking);
    }

    public Task<bool> Remove(long id)
    {
        var anime = dbContext.Anime.FindById(id);
        anime.Tracking = null;
        dbContext.Tracking.Delete(id);
        dbContext.Anime.Upsert(anime);
        return Task.FromResult(true);
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

    public void SyncList(List<AnimeModel> animeList)
    {
        var trackings = new List<LocalTracking>();
        var toUpdate = new List<LocalAnimeModel>();
        foreach (var anime in animeList)
        {
            if (anime.Tracking is null)
            {
                continue;
            }

            var localAnime = anime.ServiceName switch
            {
                nameof(AnimeId.MyAnimeList) => dbContext.Anime.FindOne(x => x.MyAnimeListId == anime.Id),
                nameof(AnimeId.Anilist) => dbContext.Anime.FindOne(x => x.AnilistId == anime.Id),
                nameof(AnimeId.AniDb) => dbContext.Anime.FindOne(x => x.AniDbId == anime.Id),
                nameof(AnimeId.Kitsu) => dbContext.Anime.FindOne(x => x.KitsuId == anime.Id),
                nameof(AnimeId.Simkl) => dbContext.Anime.FindOne(x => x.SimklId == anime.Id),
                _ => throw new NotSupportedException()
            };

            if (localAnime is null)
            {
                continue;
            }

            var localTracking = new LocalTracking
            {
                Id = localAnime.AnilistId,
                Tracking = anime.Tracking
            };

            localAnime.Tracking = localTracking;
            toUpdate.Add(localAnime);
            trackings.Add(localTracking);
        }

        dbContext.Tracking.Upsert(trackings);
        dbContext.Anime.Upsert(toUpdate);
    }
}