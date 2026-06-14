using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Anizone;

public class AnimeProvider : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await "https://anizone.to/anime"
                           .AppendQueryParam("search", query)
                           .GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var div in doc.QuerySelectorAll("div.grid > div") ?? [])
        {
            ct.ThrowIfCancellationRequested();

            var id = div.GetAttributeValue("wire:key", "")["a-".Length..];
            var image = div.QuerySelector("img").GetAttributeValue("src", "");
            var title = GetTitle(div);

            yield return new SearchResult(this, id, title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var detailsUrl = $"https://anizone.to/anime/{animeId}/";
        var stream = await detailsUrl.GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var li in doc.QuerySelectorAll("ul.grid li") ?? [])
        {
            ct.ThrowIfCancellationRequested();

            var id = li.QuerySelector("a").GetAttributeValue("href", "")[detailsUrl.Length ..];
            var title = li.QuerySelector("h3").InnerHtml.Replace($"Episode {id} :", "").Trim();
            if (!float.TryParse(id, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, id, number)
            {
                Info = new EpisodeInfo
                {
                    Titles = new Titles
                    {
                        English = title
                    }
                }
            };
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var detailsUrl = $"https://anizone.to/anime/{animeId}/{episodeId}";
        var stream = await detailsUrl.GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);

        var mediaPlayer = doc.QuerySelector("media-player");
        var src = mediaPlayer.GetAttributeValue("src", "");
        var subtitle = "";
        foreach (var track in mediaPlayer.QuerySelectorAll("track"))
        {
            var lang = track.GetAttributeValue("srclang", "");
            if (lang != "en")
            {
                continue;
            }

            subtitle = track.GetAttributeValue("src", "");
            break;
        }

        yield return new VideoServer("Default", new Uri(src))
        {
            Subtitle = subtitle
        };
    }

    private static string GetTitle(HtmlNode div)
    {
        var xData = div.GetAttributeValue("x-data", "");
        
        if (string.IsNullOrEmpty(xData))
        {
            return "";
        }

        var start = xData.IndexOf("JSON.parse('", StringComparison.Ordinal);
        
        if (start < 0)
        {
            return "";
        }

        start += "JSON.parse('".Length;
        var end = xData.IndexOf("')", start, StringComparison.Ordinal);
        
        if (end <= start)
        {
            return "";
        }

        var jsonEscaped = xData[start..end];
        var json = Regex.Unescape(jsonEscaped);

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict is { Count: > 0 })
        {
            return dict.Values.FirstOrDefault() ?? "";
        }

        return "";
    }
}