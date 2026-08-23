using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miruro;

public class AnimeProvider(
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings) : AnimeProvider<Settings>(settings)
{
    public const string BaseUrl = "https://www.miruro.tv";

    public override async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();

        var queryObject = new JsonObject
        {
            ["q"] = query,
            ["type"] = "ANIME",
            ["limit"] = 20,
            ["offset"] = 0
        };

        var json = await client.SendPipeAsync("search", queryObject, ct);
        var doc = JsonDocument.Parse(json);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var anilistId = item.GetProperty("id").GetInt64();
            var malId = item.GetProperty("idMal").GetInt64();
            var title = item.GetProperty("title").GetProperty("romaji").GetString();
            var image = item.GetProperty("coverImage").GetProperty("large").GetString();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(image))
            {
                continue;
            }

            yield return new SearchResult(this, anilistId.ToString(), title, new Uri(image))
            {
                ExternalId = new AnimeId
                {
                    Anilist = anilistId,
                    MyAnimeList = malId
                }
            };
        }
    }

    public override async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var query = new JsonObject { ["anilistId"] = int.Parse(animeId) };
        var json = await client.SendPipeAsync("episodes", query, ct);
        var root = JsonDocument.Parse(json);
        if (!root.RootElement.TryGetProperty("providers", out var providers))
        {
            yield break;
        }

        var preferredProvider = providers.GetProperty(Settings.PreferredProvider);
        foreach (var ep in ParseEpisodesFromProvider(preferredProvider))
        {
            yield return ep;
        }
    }

    public override async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId,
                                                                        [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var provider = Settings.PreferredProvider;
        var defaultSubType = Settings.PreferredSubType;

        if (string.IsNullOrEmpty(episodeId))
        {
            yield break;
        }

        var query = new JsonObject
        {
            ["episodeId"] = episodeId,
            ["provider"] = provider,
            ["category"] = defaultSubType
        };

        await foreach (var server in GetStreamServersAsync(client, query, ct))
        {
            yield return server;
        }
    }

    private static async IAsyncEnumerable<VideoServer> GetStreamServersAsync(FlurlClient client, JsonObject query,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = await client.SendPipeAsync("sources", query, cancellationToken);
        var root = JsonDocument.Parse(json);

        foreach (var streamNode in root.RootElement.GetProperty("streams").EnumerateArray())
        {
            var type = streamNode.GetProperty("type").GetString() ?? "";
            if (!type.Equals("hls", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var url = streamNode.GetProperty("url").GetString();
            var referer = streamNode.GetProperty("referer").GetString();
            var hasQuality = streamNode.TryGetProperty("quality", out var qualityNode);
            var hasServer = streamNode.TryGetProperty("server", out var serverNode);

            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            var title = "";
            if (hasQuality)
            {
                title = qualityNode.GetString();
            }
            else if (hasServer)
            {
                title = serverNode.GetString();
            }

            var server = new VideoServer(title ?? "Default", new Uri(url));

            if (!string.IsNullOrEmpty(referer))
            {
                server.Headers.TryAdd(HeaderNames.Referer, referer);
            }

            yield return server;
        }
    }

    private List<Episode> ParseEpisodesFromProvider(JsonElement providerData)
    {
        var episodesObject = providerData.GetProperty("episodes");

        string[] subTypes = Settings.PreferredProvider == "bee" ? ["ssub", "sub", "dub"] : ["sub", "dub"];
        var episodeMap = new Dictionary<float, Dictionary<string, string>>();
        var titles = new Dictionary<float, string>();

        foreach (var subType in subTypes)
        {
            foreach (var episodeNode in episodesObject.GetProperty(subType).EnumerateArray())
            {
                var number = episodeNode.GetProperty("number").GetSingle();
                var id = episodeNode.GetProperty("id").GetString();

                if (number <= 0 || string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!episodeMap.TryGetValue(number, out var subTypeIds))
                {
                    subTypeIds = new Dictionary<string, string>();
                    episodeMap[number] = subTypeIds;
                }

                subTypeIds[subType] = id;
                if (!titles.ContainsKey(number))
                {
                    titles[number] = episodeNode.GetProperty("title").GetString() ?? "";
                }
            }
        }

        return episodeMap
               .Select(pair =>
               {
                   titles.TryGetValue(pair.Key, out var title);
                   return BuildEpisode(pair.Key, title, pair.Value);
               })
               .Where(x => x is not null)
               .Select(x => x!)
               .ToList();
    }


    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}"));
    }

    private Episode? BuildEpisode(float number, string? title, Dictionary<string, string> subTypeIds)
    {
        var defaultSubType = subTypeIds.ContainsKey(Settings.PreferredSubType)
            ? Settings.PreferredSubType
            : "";

        if (string.IsNullOrWhiteSpace(defaultSubType))
        {
            return null;
        }

        return new Episode(this, "", subTypeIds[defaultSubType], number)
        {
            Info = new EpisodeInfo
            {
                Titles = new Titles
                {
                    English = title?.Trim() ?? ""
                }
            }
        };
    }
}