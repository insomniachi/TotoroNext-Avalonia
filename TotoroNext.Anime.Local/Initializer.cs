using System.Text.Json;
using Flurl.Http;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using ZstdSharp;

namespace TotoroNext.Anime.Local;

internal class Initializer(
    ILocalSettingsService localSettingsService,
    ILiteDbContext dbContext) : IBackgroundInitializer
{
    private const string OfflineDbUpdatedAtKey = "LocalDbUpdatedAt";

    public async Task BackgroundInitializeAsync()
    {
        try
        {
            var lastUpdated = localSettingsService.ReadSetting(OfflineDbUpdatedAtKey, default(DateTime));
            var stream = await "https://api.github.com/repos/manami-project/anime-offline-database/releases/latest"
                               .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                               .GetStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var date = doc.RootElement.GetProperty("published_at").GetDateTime();

            if (date > lastUpdated || !dbContext.HasData())
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
        catch
        {
            // Ignore
        }
    }

    private void Update(Stream stream)
    {
        using var decompressor = new DecompressionStream(stream);
        using var reader = new StreamReader(decompressor);
        var existing = dbContext.Anime.FindAll().ToDictionary(x => x.MyAnimeListId);

        var toUpsert = new List<LocalAnimeModel>();
        while (reader.ReadLine() is { } line)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("sources", out _))
            {
                continue;
            }

            var anime = root.Deserialize<Anime>();
            if (anime is null)
            {
                continue;
            }

            var model = LocalModelConverter.Convert(anime);
            if (model.MyAnimeListId == 0)
            {
                continue;
            }

            if (existing.TryGetValue(model.MyAnimeListId, out var existingAnime))
            {
                model.Tracking = existingAnime.Tracking;
                if (existingAnime.HasChanged(model))
                {
                    toUpsert.Add(model);
                }
            }
            else
            {
                toUpsert.Add(model);
            }
        }

        lock (dbContext)
        {
            dbContext.Anime.Upsert(toUpsert);
        }
    }
}