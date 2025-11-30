using System.Text.Json;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime;

public class OfflineDatabaseInitializer(
    ILocalSettingsService localSettingsService,
    IAnimeMappingService mappingService) : IBackgroundInitializer
{
    private const string OfflineDbUpdatedAtKey = "OfflineDbUpdatedAt";

    public async Task BackgroundInitializeAsync()
    {
        var lastUpdated = localSettingsService.ReadSetting(OfflineDbUpdatedAtKey, default(DateTime));
        var stream = await "https://api.github.com/repos/manami-project/anime-offline-database/releases/latest"
                           .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                           .GetStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var date = doc.RootElement.GetProperty("published_at").GetDateTime();

        if (date > lastUpdated || !File.Exists(FileHelper.GetPath("anime.db")))
        {
            localSettingsService.SaveSetting(OfflineDbUpdatedAtKey, date);
            var asset = doc.RootElement.GetProperty("assets")
                           .EnumerateArray()
                           .FirstOrDefault(x => x.GetProperty("name").GetString() == @"anime-offline-database.jsonl.zst");
            var url = asset.GetProperty("browser_download_url").GetString();
            var dbStream = await url.GetStreamAsync();
            mappingService.Update(dbStream);
        }
    }
}