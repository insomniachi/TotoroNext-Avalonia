using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Flurl.Http;
using FlurlGraphQL;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.AnimeHeaven;

namespace TotoroNext.Anime.AllAnime;

internal class AnimeProvider : IAnimeProvider
{
    public const string Api = "https://api.allanime.day/api";

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var jObject = await Api
            .WithGraphQLQuery(ShowQuery)
            .SetGraphQLVariable("showId", animeId)
            .PostGraphQLQueryAsync()
            .ReceiveGraphQLRawSystemTextJsonResponse();

        var episodeDetails = jObject?["show"]?["availableEpisodesDetail"] as JsonObject;

        if (episodeDetails is null)
        {
            yield break;
        }

        var details = episodeDetails.Deserialize<EpisodeDetails>();

        foreach (var episode in Enumerable.Reverse(details?.Sub ?? []))
        {
            yield return new Episode(this, animeId, episode, float.Parse(episode));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var jsonNode = await Api
            .WithGraphQLQuery(EpisodeQuery)
            .SetGraphQLVariables(new
            {
                showId = animeId,
                translationType = "sub",
                episodeString = episodeId
            })
            .PostGraphQLQueryAsync()
            .ReceiveGraphQLRawSystemTextJsonResponse();

        if (jsonNode?["errors"] is not null)
        {
            yield break;
        }

        var sourceArray = jsonNode?["episode"]?["sourceUrls"];
        var sourceObjs = sourceArray?.Deserialize<List<SourceUrlObj>>() ?? [];
        sourceObjs.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var item in sourceObjs)
        {
            if (item.SourceUrl.StartsWith("--"))
            {
                item.SourceUrl = DecryptSourceUrl(item.SourceUrl);
            }

            switch (item.SourceName)
            {
                case "Mp4":
                    if (await VideoServers.FromMp4Upload(item.SourceName, item.SourceUrl) is { } server)
                    {
                        yield return server;
                    }
                    continue;
                case "Yt-mp4":
                    yield return VideoServers.WithReferer(item.SourceName, item.SourceUrl, "https://allanime.day/");
                    continue;
                case "Vg":
                case "Fm-Hls":
                case "Sw":
                case "Ok":
                case "Ss-Hls":
                case "Vid-mp4":
                    continue;
            }

            JsonObject? jObject;
            try
            {
                var response = await $"https://allanime.day{item.SourceUrl.Replace("clock", "clock.json")}".GetStringAsync();
                jObject = JsonNode.Parse(response)!.AsObject();
            }
            catch
            {
                continue;
            }

            switch (item.SourceName)
            {
                case "Luf-Mp4" or "S-mp4":
                    var links = jObject["links"].Deserialize<List<ApiV2Response>>() ?? [];
                    yield return VideoServers.WithReferer(item.SourceName, links[0].Url, "https://allanime.day/");
                    continue;
                case "Default":
                    var hls = jObject["links"].Deserialize<List<DefaultResponse>>() ?? [];
                    yield return new VideoServer(item.SourceName, new Uri(hls[0].Link));
                    continue;
            }

        }
    }

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var jObject = await Api
             .WithGraphQLQuery(SearchQuery)
             .SetGraphQLVariables(new
             {
                 search = new
                 {
                     allowAdult = true,
                     allowUnknown = true,
                     query
                 },
                 limit = 40
             })
             .PostGraphQLQueryAsync()
             .ReceiveGraphQLRawSystemTextJsonResponse();

        foreach (var item in jObject?["shows"]?["edges"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            var title = $"{item["name"]}";
            var id = $"{item["_id"]}";
            Uri? image = null;
            try
            {
                image = new Uri($"{item["thumbnail"]}");
            }
            catch
            {
                // ignored
            }

            yield return new SearchResult(this, id, title, image);
        }
    }

    private static string Decrypt(string target) => string.Join("", Convert.FromHexString(target).Select(x => (char)(x ^ 56)));

    private static string DecryptSourceUrl(string sourceUrl)
    {
        var index = sourceUrl.LastIndexOf('-') + 1;
        var encrypted = sourceUrl[index..];
        return Decrypt(encrypted);
    }

    public const string SearchQuery =
    $$"""
        query( $search: SearchInput
               $limit: Int
               $page: Int
               $translationType: VaildTranslationTypeEnumType
               $countryOrigin: VaildCountryOriginEnumType )
        {
            shows( search: $search
                    limit: $limit
                    page: $page
                    translationType: $translationType
                    countryOrigin: $countryOrigin )
            {
                pageInfo
                {
                    total
                }
                edges 
                {
                    _id,
                    name,
                    availableEpisodesDetail,
                    season,
                    score,
                    thumbnail,
                    malId,
                    aniListId
                }
            }
        }
        """;

    public const string ShowQuery =
    """
        query ($showId: String!) {
            show(
                _id: $showId
            ) {
                availableEpisodesDetail,
                malId,
                aniListId
            }
        }
        """;

    public const string EpisodeQuery =
    """
        query ($showId: String!, $translationType: VaildTranslationTypeEnumType!, $episodeString: String!) {
            episode(
                showId: $showId
                translationType: $translationType
                episodeString: $episodeString
            ) {
                episodeString,
                sourceUrls,
                notes
            }
        }
        """;
}

internal sealed class EpisodeDetails
{
    [JsonPropertyName("sub")]
    public List<string> Sub { get; init; } = [];

    [JsonPropertyName("dub")]
    public List<string> Dub { get; init; } = [];

    [JsonPropertyName("raw")]
    public List<string> Raw { get; init; } = [];
}

[DebuggerDisplay("{Priority} - {SourceUrl} - {Type}")]
[UsedImplicitly]
internal sealed class SourceUrlObj
{

    [JsonPropertyName("sourceName")]
    public string SourceName { get; set; } = "";

    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = "";

    [JsonPropertyName("priority")]
    public double Priority { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

[UsedImplicitly]
public sealed class ApiV2Response
{
    [JsonPropertyName("src")]
    public string Url { get; set; } = "";

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = [];
}

[UsedImplicitly]
public sealed class DefaultResponse
{
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
    
    [JsonPropertyName("hls")]
    public bool Hls { get; set; }
    
    [JsonPropertyName("resolutionStr")]
    public string ResolutionString { get; set; } = string.Empty;
    
    [JsonPropertyName("resolution")]
    public int Resolution { get; set; } = 0;
    
    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;
}
