using System.Runtime.CompilerServices;
using System.Text.Json;
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
    
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
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
                               .GetStringAsync(cancellationToken: ct);

        if (string.IsNullOrEmpty(json))
        {
            yield break;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in dataArray.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            
            var title = item.GetProperty("title").GetString() ?? "";
            var poster = item.GetProperty("poster").GetString() ?? "";
            var session = item.GetProperty("session").GetString() ?? "";

            if (!Uri.TryCreate(poster, UriKind.Absolute, out var image))
            {
                continue;
            }

            yield return new SearchResult(this, session, title, image);
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = GetClient();
        var stream = await client.Request("anime", animeId).GetStreamAsync(cancellationToken: ct);
        var doc = new HtmlDocument();
        doc.Load(stream);

        var releaseId = IdRegex().Match(doc.Text).Groups[1].Value;
        var page = await GetSessionPage(client, releaseId, 1, ct);

        for (var pageNumber = 1; pageNumber <= page.LastPage; pageNumber++)
        {
            if (pageNumber != 1)
            {
                page = await GetSessionPage(client, releaseId, pageNumber, ct);
            }

            foreach (var ep in page.Data)
            {
                ct.ThrowIfCancellationRequested();
                yield return new Episode(this, releaseId, ep.Session, (float)ep.Episode);
            }
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = GetClient();
        var streamData = await client.Request("play", animeId, episodeId).GetStreamAsync(cancellationToken: ct);
        var doc = new HtmlDocument();
        doc.Load(streamData);

        var nodes = doc.QuerySelectorAll("#pickDownload .dropdown-item") ?? [];
        var servers = nodes
                      .Select(x => new
                      {
                          Title = x.InnerText,
                          Resolution = ExtractResolution(x.InnerText),
                          IsDub = x.InnerText.EndsWith("eng"),
                          Url = x.Attributes["href"].Value
                      })
                      .OrderByDescending(x => x.Resolution)
                      .ThenBy(x => x.IsDub)
                      .ToList();

        foreach (var server in servers)
        {
            ct.ThrowIfCancellationRequested();
            yield return new VideoServer(server.Title.Replace("&middot;", "•"), new Uri(server.Url), _extractor);
        }
    }

    private static int ExtractResolution(string item)
    {
        var parts = item.Split("&middot;");
        if (parts.Length < 2)
        {
            return 0;
        }

        var resPart = parts[1].Trim().Split(' ')[0]; // e.g., "360p"
        return int.TryParse(resPart.Replace("p", ""), out var res) ? res : 0;
    }

    [GeneratedRegex("let id = \"(.+?)\"", RegexOptions.Compiled)]
    private static partial Regex IdRegex();


    private static async Task<AnimePaheEpisodePage> GetSessionPage(FlurlClient client, string releaseId, int page, CancellationToken ct)
    {
        return await client.Request("api").SetQueryParams(new
        {
            m = "release",
            id = releaseId,
            sort = "episode_asc",
            page
        }).GetJsonAsync<AnimePaheEpisodePage>(cancellationToken: ct);
    }

    private FlurlClient GetClient()
    {
        var client = httpClientFactory.CreateClient(Module.Descriptor.Id.ToString());
        return new FlurlClient(client);
    }
}