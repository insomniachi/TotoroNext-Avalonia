using System.Text.Json;
using Flurl.Http;
using LiteDB;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using ZstdSharp;

namespace TotoroNext.Anime.Local;

public class Initializer(ILocalSettingsService localSettingsService) : IBackgroundInitializer
{
    private const string OfflineDbUpdatedAtKey = "LocalDbUpdatedAt";

    public async Task BackgroundInitializeAsync()
    {
        var lastUpdated = localSettingsService.ReadSetting(OfflineDbUpdatedAtKey, default(DateTime));
        var stream = await "https://api.github.com/repos/manami-project/anime-offline-database/releases/latest"
                           .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                           .GetStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var date = doc.RootElement.GetProperty("published_at").GetDateTime();

        if (date > lastUpdated || !File.Exists(FileHelper.GetPath("animeData.db")))
        {
            localSettingsService.SaveSetting(OfflineDbUpdatedAtKey, date);
            var asset = doc.RootElement.GetProperty("assets")
                           .EnumerateArray()
                           .FirstOrDefault(x => x.GetProperty("name").GetString() == @"anime-offline-database.jsonl.zst");
            var url = asset.GetProperty("browser_download_url").GetString();
            var dbStream = await url.GetStreamAsync();
            Update(dbStream);
        }
    }

    private static void Update(Stream stream)
    {
        using var decompressor = new DecompressionStream(stream);
        using var reader = new StreamReader(decompressor);
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalAnimeModel>();
        var existing = collection.FindAll().ToDictionary(x => x.AnilistId);
        collection.EnsureIndex(x => x.MyAnimeListId);

        var toUpsert = new List<LocalAnimeModel>();
        while (reader.ReadLine() is { } line)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("sources", out var sources))
            {
                continue;
            }

            var anime = root.Deserialize<Anime>();
            if (anime is null)
            {
                continue;
            }

            var model = LocalModelConverter.Convert(anime);
            if (existing.TryGetValue(model.AnilistId, out var existingAnime))
            {
                if (model.TotalEpisodes != existingAnime.TotalEpisodes)
                {
                    toUpsert.Add(model);
                }
            }
            else
            {
                toUpsert.Add(model);
            }
        }

        collection.Upsert(toUpsert);
    }
}