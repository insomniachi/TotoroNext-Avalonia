using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.AnimePahe;

public partial class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    private readonly KwikExtractor _extractor = new(httpClientFactory);

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        using var client = GetClient();

        var json = await client.Request()
                               .AppendPathSegment("api")
                               .SetQueryParams(new
                               {
                                   m = "search",
                                   q = query,
                                   l = 8
                               })
                               .GetStringAsync();

        if (string.IsNullOrEmpty(json))
        {
            yield break;
        }

        var jObject = JsonNode.Parse(json);

        if (jObject?["data"]?.AsArray() is not { } results)
        {
            yield break;
        }

        foreach (var item in results)
        {
            var title = $"{item?["title"]}";
            Uri? image;
            try
            {
                image = new Uri($"{item?["poster"]}");
            }
            catch
            {
                continue;
            }

            var id = $"{item?["session"]}";

            yield return new SearchResult(this, id, title, image);
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        using var client = GetClient();
        var stream = await client.Request("anime", animeId).GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var releaseId = IdRegex().Match(doc.Text).Groups[1].Value;
        var page = await GetSessionPage(client, releaseId, 1);

        for (var pageNumber = 1; pageNumber <= page.LastPage; pageNumber++)
        {
            if (pageNumber != 1)
            {
                page = await GetSessionPage(client, releaseId, pageNumber);
            }

            foreach (var ep in page.Data)
            {
                yield return new Episode(this, releaseId, ep.Session, (float)ep.Episode);
            }
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        using var client = GetClient();
        var streamData = await client.Request("play", animeId, episodeId).GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(streamData);

        var nodes = doc.QuerySelectorAll("#pickDownload .dropdown-item") ?? [];

        foreach (var node in nodes)
        {
            yield return new VideoServer(node.InnerText.Replace("&middot;", "•"), new Uri(node.Attributes["href"].Value), _extractor);
        }
    }

    [GeneratedRegex("let id = \"(.+?)\"", RegexOptions.Compiled)]
    private static partial Regex IdRegex();


    private static async Task<AnimePaheEpisodePage> GetSessionPage(FlurlClient client, string releaseId, int page)
    {
        return await client.Request("api").SetQueryParams(new
        {
            m = "release",
            id = releaseId,
            sort = "episode_asc",
            page
        }).GetJsonAsync<AnimePaheEpisodePage>();
    }

    private FlurlClient GetClient()
    {
        var client = httpClientFactory.CreateClient(Module.Descriptor.Id.ToString());
        return new FlurlClient(client);
    }
}