using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Labs;

public partial class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public const string BaseUrl = "https://av1encodes.com/";

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await BaseUrl.AppendPathSegment("search_suggestions")
                                  .AppendQueryParam("q", query)
                                  .WithRequiredHeaders()
                                  .GetStreamAsync(cancellationToken: ct);

        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        foreach (var item in doc.RootElement.GetProperty("suggestions").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString();
            var title = item.GetProperty("name").GetString();
            var image = item.GetProperty("image").GetString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title))
            {
                yield break;
            }

            yield return new SearchResult(this, id, title, new Uri(image ?? ""));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, CancellationToken ct)
    {
        var stream = await BaseUrl.AppendPathSegment($"anime/{animeId}")
                                  .WithRequiredHeaders()
                                  .GetStreamAsync(cancellationToken: ct);
        var doc = new HtmlDocument();
        doc.Load(stream);

        string resolution = "1920 x 1080"; // Ideally fetched from user settings
        
        foreach (var season in doc.QuerySelectorAll(".season-tab[data-season], .season-option[data-season], [data-season]"))
        {
            var seasonString = season.GetAttributeValue("data-season", "");
            if (!int.TryParse(seasonString, out var number))
            {
                continue;
            }

            var encodedRes = Uri.EscapeDataString(resolution);
            stream = await BaseUrl.AppendPathSegment($"episodes/{animeId}/{number}/{encodedRes}")
                                  .WithRequiredHeaders()
                                  .GetStreamAsync(cancellationToken: ct);
            doc.Load(stream);

            foreach (var ep in doc.QuerySelectorAll("a[href*='/download/']"))
            {
                var href = ep.GetAttributeValue("href", "");
                if (EpisodeNumberRegex().Match(href) is not { Success: true } match)
                {
                    continue;
                }

                yield return new Episode(this, animeId, href, int.Parse(match.Groups[1].Value));
            }

            yield break;
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, CancellationToken ct)
    {
        var path = BaseUrl.AppendPathSegment(episodeId);
        var content = await FetchDownloadLinkPage(path);

        var fileName = episodeId.Split("/").Last().Split("?").First();
        if (DdlTokenRegex().Match(content) is not { Success: true } match)
        {
            yield break;
        }
        var encodedFileName = Uri.EscapeDataString(fileName).Replace("%20", "+");
        var stream = await FetchDdlPage($"{BaseUrl}get_ddl/{encodedFileName}", path, match.Groups[1].Value);

        yield break;
    }

    private async Task<string> FetchDownloadLinkPage(string url)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers =
            {
                { "accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
                { "accept-language", "en-US,en;q=0.9" },
                { "priority", "u=0, i" },
                { "referer", "https://av1encodes.com/" },
                { "sec-ch-ua", "\"Not;A=Brand\";v=\"8\", \"Chromium\";v=\"150\", \"Microsoft Edge\";v=\"150\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "document" },
                { "sec-fetch-mode", "navigate" },
                { "sec-fetch-site", "same-origin" },
                { "sec-fetch-user", "?1" },
                { "upgrade-insecure-requests", "1" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36 Edg/150.0.0.0" },
            },
        };
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    
    private async Task<string> FetchDdlPage(string url, string referer, string token)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers =
            {
                { "accept", "application/json" },
                { "accept-language", "en-US,en;q=0.9" },
                { "referer", referer },
                { "sec-ch-ua", "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "document" },
                { "sec-fetch-mode", "navigate" },
                { "sec-fetch-site", "none" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36" },
                { "x-ddl-token",  token },
            },
        };
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }


    [GeneratedRegex(@"([a-zA-Z0-9_ \-\[\]().%]+?\.(?:mkv|mp4))")]
    private static partial Regex FileNamesRegex();

    [GeneratedRegex(@"E(\d+)")]
    private static partial Regex EpisodeNumberRegex();

    [GeneratedRegex("['\"](A{4,}[A-Za-z0-9_\\-]{10,})['\"]")]
    private static partial Regex DdlTokenRegex();

    private static string ExtractFileName(string url)
    {
        return "";
    }
}