using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Downloader;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anikoto;

public partial class AnimeProvider(
    IHttpClientFactory httpClientFactory,
    [FromKeyedServices(DownloaderTypes.Ytdlp)] IAnimeDownloader downloader,
    IModuleSettings<Settings> settings) : IAnimeProvider, IDownloadableAnimeProvider
{
    public const string BaseUrl = "https://anikototv.to/";

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient("api");

        var vrf = string.IsNullOrEmpty(query) ? "" : Utils.VrfEncrypt(query);

        var stream = await client.Request("filter")
                                 .SetQueryParam("keyword", query)
                                 .SetQueryParam("page", 1)
                                 .SetQueryParam("vrf", vrf)
                                 .GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);
        foreach (var node in doc.QuerySelectorAll("div.item"))
        {
            var imgNode = node.QuerySelector("img");
            var image = imgNode?.GetAttributeValue("src", "");
            var title = imgNode?.GetAttributeValue("alt", "");
            var id = node.QuerySelector(".tip").GetAttributeValue("data-tip", "");
            var url = new Uri(node.QuerySelector(".d-title").GetAttributeValue("href", ""));
            var siteId = url.Segments[^2][.. ^1];

            if (title is null || image is null)
            {
                continue;
            }

            yield return new SearchResult(this, $"{id}:{siteId}", title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient("api");
        var idNumber = animeId.Split(":")[0];

        var stream = await client.Request("ajax/episode/list")
                                 .AppendPathSegment(idNumber)
                                 .GetStreamAsync(cancellationToken: ct);

        var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var result = jsonDoc.RootElement.GetProperty("result").GetString();
        var doc = new HtmlDocument();
        doc.LoadHtml(result ?? "");

        foreach (var node in doc.QuerySelectorAll(".episodes.name a"))
        {
            var numberString = node.GetAttributeValue("data-num", "");
            var id = node.GetAttributeValue("data-ids", "");

            if (!float.TryParse(numberString, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, $"{id}:{number}", number);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient("api");
        var episodeIdParts = episodeId.Split(':');

        var stream = await client.Request("ajax/server/list")
                                 .AppendQueryParam("servers", episodeIdParts[0])
                                 .GetStreamAsync(cancellationToken: ct);

        var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var html = jsonDoc.RootElement.GetProperty("result").GetString();
        var doc = new HtmlDocument();
        doc.LoadHtml(html ?? "");

        var preferredCategory = settings.Value.PreferredStreamCategory;
        var preferredServer = settings.Value.PreferredServer;

        foreach (var node in doc.QuerySelectorAll($".type[data-type='{preferredCategory}']"))
        {
            foreach (var li in node.QuerySelectorAll("li"))
            {
                var id = li.GetAttributeValue("data-link-id", "");

                if (id.StartsWith("http"))
                {
                    continue;
                }

                var response = await client.Request("ajax/server")
                                           .AppendQueryParam("get", id)
                                           .GetJsonAsync<StreamResponse>(cancellationToken: ct);

                var host = new Uri(response.Result.Url).Host;

                var content = await response.Result.Url
                                            .WithHeader(HttpHeaderNames.Referer, "https://anikototv.to/")
                                            .GetStringAsync(cancellationToken: ct);

                if (DataIdRegex().Match(content) is not { Success: true } match)
                {
                    continue;
                }

                var dataId = match.Groups[1].Value;
                var dataResponse = await $"https://{host}/stream/getSources"
                                         .AppendQueryParam("id", dataId)
                                         .WithHeader(HttpHeaderNames.Referer, response.Result.Url)
                                         .WithHeader("X-Requested-With", "XMLHttpRequest")
                                         .GetJsonAsync<PlayerResponse>(cancellationToken: ct);

                yield return new VideoServer(li.InnerHtml, new Uri(dataResponse.Sources.File))
                {
                    Headers =
                    {
                        [HttpHeaderNames.Referer] = $"https://{host}/"
                    },
                    Subtitle = dataResponse.Tracks.FirstOrDefault(x => x.Default)?.File,
                    IsDefault = li.InnerHtml == preferredServer,
                    ContentType = "ts"
                };
            }
        }
    }

    public IAnimeDownloader GetDownloader() => downloader;

    private FlurlClient CreateClient(string name)
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}-{name}"));
    }

    [GeneratedRegex("data-id=\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex DataIdRegex();
}