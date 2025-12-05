using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl.Http;
using FlurlGraphQL;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime;

internal class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var jObject = await GraphQl.Api
                                   .WithGraphQLQuery(GraphQl.ShowQuery)
                                   .SetGraphQLVariable("showId", animeId)
                                   .PostGraphQLQueryAsync()
                                   .ReceiveGraphQLRawSystemTextJsonResponse();

        if (jObject?["show"]?["availableEpisodesDetail"] is not JsonObject episodeDetails)
        {
            yield break;
        }

        var details = episodeDetails.Deserialize<EpisodeDetails>();

        foreach (var episode in Enumerable.Reverse(GetEpisodeDetails(details, settings.Value.TranslationType)))
        {
            yield return new Episode(this, animeId, episode, float.Parse(episode));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var jsonNode = await GraphQl.Api
                                    .WithGraphQLQuery(GraphQl.EpisodeQuery)
                                    .SetGraphQLVariables(new
                                    {
                                        showId = animeId,
                                        translationType = GetTranslationType(settings.Value.TranslationType),
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
                    yield return VideoServers.WithReferer(item.SourceName, item.SourceUrl, "https://allanime.day/")
                                             .WithContentType("mp4");
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
                    if (!string.IsNullOrEmpty(links[0].Url))
                    {
                        yield return VideoServers.WithReferer(item.SourceName, links[0].Url, "https://allanime.day/");
                    }

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
        var jObject = await GraphQl.Api
                                   .WithGraphQLQuery(GraphQl.SearchQuery)
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
            long malId = 0;
            long anilistId = 0;
            try
            {
                image = new Uri($"{item["thumbnail"]}");
                malId = long.Parse($"{item["malId"]}");
                anilistId = long.Parse($"{item["aniListId"]}");
            }
            catch
            {
                // ignored
            }

            yield return new SearchResult(this, id, title, image)
            {
                ExternalId = new AnimeId
                {
                    MyAnimeList = malId,
                    Anilist = anilistId
                }
            };
        }
    }

    public List<ModuleOptionItem> GetOptions()
    {
        return settings.Value.ToModuleOptions();
    }

    public void UpdateOptions(List<ModuleOptionItem> options)
    {
        settings.Value.UpdateValues(options);
    }

    private static string Decrypt(string target)
    {
        return string.Join("", Convert.FromHexString(target).Select(x => (char)(x ^ 56)));
    }

    private static string DecryptSourceUrl(string sourceUrl)
    {
        var index = sourceUrl.LastIndexOf('-') + 1;
        var encrypted = sourceUrl[index..];
        return Decrypt(encrypted);
    }

    private static List<string> GetEpisodeDetails(EpisodeDetails? details, TranslationType type)
    {
        if (details is null)
        {
            return [];
        }

        return type switch
        {
            TranslationType.Dub => details.Dub,
            TranslationType.Raw => details.Raw,
            _ => details.Sub
        };
    }

    private static string GetTranslationType(TranslationType type)
    {
        return type switch
        {
            TranslationType.Raw => "raw",
            TranslationType.Dub => "dub",
            _ => "sub"
        };
    }
}