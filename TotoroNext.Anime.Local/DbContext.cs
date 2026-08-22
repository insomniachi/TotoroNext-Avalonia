using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using GraphQL.Client.Http;
using JetBrains.Annotations;
using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Local.Mapping;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using ZstdSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TotoroNext.Anime.Local;

[UsedImplicitly]
internal class DbContext(GraphQLHttpClient client,
                         IHttpClientFactory httpClientFactory,
                         IModuleSettings<Settings> settings) : IDbContext
{
    private readonly LiteDatabase _db = new(FileHelper.GetPath("anime.db"));

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ILiteCollection<OfflineAnimeModel> Anime => _db.GetCollection<OfflineAnimeModel>().IncludeExtras();
    public ILiteCollection<LocalTracking> Tracking => _db.GetCollection<LocalTracking>();

    public async Task DownloadAllSeasonsFromCache()
    {
        var page = 1;
        while (true)
        {
            try
            {
                var stream = await $"https://api.github.com/repos/insomniachi/OfflineAnimeDatabase/releases?page={page}&per_page=100"
                                   .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                                   .GetStreamAsync();
                var doc = await JsonDocument.ParseAsync(stream);
                var releases = doc.RootElement.EnumerateArray().ToList();

                if (releases.Count is 0)
                {
                    break;
                }

                foreach (var release in releases)
                {
                    await DownloadUpdateCache(release);
                }

                if (releases.Count < 100)
                {
                    break;
                }

                page++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public async Task DownloadSeasonFromCache(int year, AnimeSeason season)
    {
        var stream = await $"https://api.github.com/repos/insomniachi/OfflineAnimeDatabase/releases/tags/{year}.{(int)season + 1}"
                           .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                           .GetStreamAsync();

        var doc = await JsonDocument.ParseAsync(stream);
        await DownloadUpdateCache(doc.RootElement);
    }
    
    public async Task DownloadSeason(int year, AnimeSeason season)
    {
        var anidb = new Anidb(httpClientFactory);
        var ann = new AnimeNewsNetwork(httpClientFactory);
        var kitsu = new Kitsu(httpClientFactory);

        var currentSeason = AnimeHelpers.CurrentSeason();

        if (year > currentSeason.Year || (year == currentSeason.Year && season >= currentSeason.SeasonName))
        {
            await anidb.CacheAniDbTitlesAsync();
            await ann.Initialize();
        }
        
        await foreach (var list in AnilistHelper.GetSeasonalAnime(client, year, season))
        {
            foreach (var media in list)
            {
                var model = Converter.ToDbModel(media);
                try
                {
                    model.AnnId = ann.TryGetId(model);
                    model.AniDbId = anidb.TryGetId(model);
                    model.KitsuId = await kitsu.TryGetId(model);
                    model.SimklId = await Simkl.TryGetId(model, settings.Value.SimklClientId);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Anime.Upsert(model);
            }
        }
    }
    

    private async Task DownloadUpdateCache(JsonElement release)
    {
        // var tag = release.GetProperty("tag_name").GetString();
        // var name = release.GetProperty("name").GetString();
        var asset = release.GetProperty("assets").EnumerateArray().ElementAt(0).GetProperty("browser_download_url").GetString();

        if (string.IsNullOrEmpty(asset))
        {
            return;
        }

        var assetStream = await asset.GetStreamAsync();
        foreach (var anime in ParseCache<AnimeModelRemote>(assetStream, _options))
        {
            var model = Converter.ToDbModel(anime);
            Anime.Upsert(model);
        }
    }

    private static IEnumerable<T> ParseCache<T>(Stream stream, JsonSerializerOptions options)
    {
        using var decompressorStream = new DecompressionStream(stream);
        using var reader = new StreamReader(decompressorStream);

        while (reader.ReadLine() is { } line)
        {
            var item = JsonSerializer.Deserialize<T>(line, options);
            if (item != null)
            {
                yield return item;
            }
        }
    }
}

internal interface IDbContext
{
    ILiteCollection<OfflineAnimeModel> Anime { get; }
    ILiteCollection<LocalTracking> Tracking { get; }
    Task DownloadAllSeasonsFromCache();
    Task DownloadSeasonFromCache(int year, AnimeSeason season);
    Task DownloadSeason(int year, AnimeSeason season);
}