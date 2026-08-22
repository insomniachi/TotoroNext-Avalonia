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

public partial class AnimeProvider : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var html = await "https://anizone.to/anime"
                           .AppendQueryParam("search", query)
                           .GetStringAsync(cancellationToken: ct);

        var match = JsonListRegex().Match(html);
        
        if (!match.Success)
        {
            Console.WriteLine("Could not find the JSON data payload using Regex.");
            yield break;
        }

        var escapedJson = match.Groups[1].Value;
        var decodedJson = Regex.Unescape(escapedJson);
        var doc = JsonDocument.Parse(decodedJson);

        foreach (var anime in doc.RootElement.EnumerateArray())
        {
            var id = anime.GetProperty("slug").GetString()!;
            var image = anime.GetProperty("cover").GetString()!;
            var title = anime.GetProperty("main_title").GetString()!;
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
            if (!float.TryParse(id, out var number))
            {
                continue;
            }
            
            var ep = new Episode(this, animeId, id, number);
            var data = li.GetAttributeValue("x-data", "");
            var match = JsonObjectRegex().Match(data);
            if (!match.Success)
            {
                yield return ep;
            }

            var escapedJson = match.Groups[1].Value;
            var decodedJson = Regex.Unescape(escapedJson);
            var titles = JsonDocument.Parse(decodedJson);
            ep.Info = new EpisodeInfo()
            {
                Titles = new Titles()
                {
                    English = titles.RootElement.EnumerateObject().Last().Value.GetString()!
                }
            };
            yield return ep;
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var detailsUrl = $"https://anizone.to/anime/{animeId}/{episodeId}";
        var stream = await detailsUrl.GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);
        
        var match = VideoRegex().Match(doc.Text);
        var escapedJson = match.Groups[1].Value;
        var decodedJson = Regex.Unescape(escapedJson);
        var videos = JsonDocument.Parse(decodedJson);
        var src = videos.RootElement.GetProperty("src").GetString()!;
        var subtitle = "";
        SkipData? data = null;
        if (videos.RootElement.TryGetProperty("chapter", out var property))
        {
            var url = property.GetString()!;
            var vtt = await url.GetStringAsync(cancellationToken: ct);
            data = ParseVtt(vtt);
        }
        
        foreach (var track in videos.RootElement.GetProperty("subtitles").EnumerateArray())
        {
            var lang = track.GetProperty("language").GetString()!;
            if (lang != "en")
            {
                continue;
            }

            subtitle = track.GetProperty("file").GetString();
            break;
        }

        yield return new VideoServer("Default", new Uri(src))
        {
            Subtitle = subtitle,
            SkipData = data
        };
    }
    
    public static SkipData ParseVtt(string vttText)
    {
        var result = new SkipData();
        var blocks = VttRegex().Split(vttText.Trim());

        foreach (var block in blocks)
        {
            // Skip the WEBVTT header block
            if (block.StartsWith("WEBVTT")) continue;

            var lines = block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                continue;
            }

            var timeLine = lines[0]; // e.g., 00:00:00.000 --> 00:00:05.041
            var textLine = lines[1]; // e.g., Intro

            // Target only "Intro" (Openings) or "Credits" (Edits)
            if (!textLine.Equals("Intro", StringComparison.OrdinalIgnoreCase) &&
                !textLine.Equals("Credits", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var timeParts = timeLine.Split(["-->"], StringSplitOptions.RemoveEmptyEntries);
            if (timeParts.Length != 2)
            {
                continue;
            }

            var startTime = TimeSpan.Parse(timeParts[0].Trim());
            var endTime = TimeSpan.Parse(timeParts[1].Trim());
            if (textLine.Equals("Intro", StringComparison.OrdinalIgnoreCase))
            {
                result.Opening = new Segment()
                {
                    Start = startTime,
                    End = endTime
                };
            }
            if (textLine.Equals("Credits", StringComparison.OrdinalIgnoreCase))
            {
                result.Ending = new Segment()
                {
                    Start = startTime,
                    End = endTime
                };
            }
        }
        
        return result;
    }

    [GeneratedRegex("""x-data="[^"]*items:\s*JSON\.parse\('(.*?)'\)""", RegexOptions.Singleline)]
    private partial Regex JsonListRegex();

    [GeneratedRegex(@"JSON\.parse\(\s*'([^']+)'\s*\)")]
    private partial Regex JsonObjectRegex();

    [GeneratedRegex(@"vidstackPlayer\(JSON\.parse\(\s*'([^']+)'\s*\)\)")]
    private partial Regex VideoRegex();
    
    [GeneratedRegex(@"\r\n\r\n|\n\n|\r\r")]
    private static partial Regex VttRegex();
}