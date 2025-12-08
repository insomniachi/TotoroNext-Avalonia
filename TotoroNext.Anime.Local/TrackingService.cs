using System.Xml;
using Avalonia.Platform.Storage;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Local;

internal class TrackingService(ILiteDbContext dbContext,
                               IStorageProvider storageProvider) : ILocalTrackingService
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

    public async Task ExportList(List<AnimeModel> animeList)
    {
        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false
        });

        if (folders.Count <= 0)
        {
            return;
        }

        var file = Path.Combine(folders[0].Path.LocalPath, "totoro_list.xml");
        await using var writer = XmlWriter.Create(file, new XmlWriterSettings { Indent = true });
        await writer.WriteStartDocumentAsync();
        writer.WriteStartElement("myanimelist");
        writer.WriteStartElement("myinfo");
        writer.WriteElementString("user_id", "");
        writer.WriteElementString("user_export_type", "1");
        writer.WriteElementString("user_total_anime", animeList.Count.ToString());
        writer.WriteElementString("user_total_watching", animeList.Count(x => x.Tracking?.Status == ListItemStatus.Watching).ToString());
        writer.WriteElementString("user_total_completed", animeList.Count(x => x.Tracking?.Status == ListItemStatus.Completed).ToString());
        writer.WriteElementString("user_total_onhold", animeList.Count(x => x.Tracking?.Status == ListItemStatus.OnHold).ToString());
        writer.WriteElementString("user_total_dropped", animeList.Count(x => x.Tracking?.Status == ListItemStatus.Dropped).ToString());
        writer.WriteElementString("user_total_plantowatch", animeList.Count(x => x.Tracking?.Status == ListItemStatus.PlanToWatch).ToString());
        await writer.WriteEndElementAsync();
        foreach (var anime in animeList)
        {
            writer.WriteStartElement("anime");

            writer.WriteElementString("series_animedb_id", anime.ExternalIds.MyAnimeList.ToString());

            if (anime.Tracking is { } tracking)
            {
                if (tracking.WatchedEpisodes > 0)
                {
                    writer.WriteElementString("my_watched_episodes", anime.Tracking.WatchedEpisodes.ToString());
                }

                if (tracking.StartDate is { } sd)
                {
                    writer.WriteElementString("my_start_date", sd.ToString("yyyy-MM-dd"));
                }

                if (tracking.FinishDate is { } fd)
                {
                    writer.WriteElementString("my_finish_date", fd.ToString("yyyy-MM-dd"));
                }

                if (tracking.Score > 0)
                {
                    writer.WriteElementString("my_score", tracking.Score.ToString());
                }

                if (tracking.Status is { } status)
                {
                    writer.WriteElementString("my_status", ConvertStatus(status));
                }
            }

            writer.WriteElementString("update_on_import", "1");

            await writer.WriteEndElementAsync();
        }
        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        return;

        static string ConvertStatus(ListItemStatus status)
        {
            return status switch
            {
                ListItemStatus.Watching => "Watching",
                ListItemStatus.Completed => "Completed",
                ListItemStatus.Dropped => "Dropped",
                ListItemStatus.OnHold => "On-Hold",
                ListItemStatus.PlanToWatch => "Plan to Watch",
                _ => throw new NotSupportedException()
            };
        }
    }
}