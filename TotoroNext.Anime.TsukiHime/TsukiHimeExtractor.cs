using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.TsukiHime;

public partial class TsukiHimeExtractor(ITorrentExtractor torrentExtractor) : IVideoExtractor
{
    public async IAsyncEnumerable<VideoSource> Extract(Uri url, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await url.GetStreamAsync(cancellationToken: ct);
        var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var filesElement = jsonDocument.RootElement.GetProperty("files");
        var files = filesElement.Deserialize<List<TorrentContent>>() ?? [];
        
        var torrentSource = await TorrentAsVideoSource(jsonDocument);;
        
        if (files is not { Count: > 0 } && torrentSource is not null)
        {
            yield return torrentSource;
        }

        var biggestFile = files.MaxBy(x => x.Size);
        var source = await ExtractFileDitch(biggestFile?.Links.FileDitch, ct);
        if (source is not null)
        {
            yield return source;
        }

        if (torrentSource is not null)
        {
            yield return torrentSource;
        }
    }

    private async Task<VideoSource?> TorrentAsVideoSource(JsonDocument jsonDocument)
    {
        var id = jsonDocument.RootElement.GetProperty("nyaa_id").GetInt64();
        var url = new Uri($"https://nyaa.si/download/{id}.torrent");
        return await torrentExtractor.Extract(url, CancellationToken.None).FirstOrDefaultAsync();
    }

    private static async Task<VideoSource?> ExtractFileDitch(string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }
        
        var stream = await url.GetStreamAsync(cancellationToken: ct);
        var doc = new HtmlDocument();
        doc.Load(stream);
        var scriptTag = doc.QuerySelectorAll("script").FirstOrDefault(x => x.InnerText.Contains("function(d)"));
        var streamUrl = ParseUrl(scriptTag?.InnerText ?? "");
        if (string.IsNullOrEmpty(streamUrl))
        {
            return null;
        }

        return new VideoSource
        {
            Url = new Uri(streamUrl),
            DownloaderType = DownloaderTypes.Http
        };
    }

    private static string ParseUrl(string input)
    {
        var match = SourceRegex().Match(input);

        if (!match.Success)
        {
            return "";
        }

        var arrayContents = match.Groups[1].Value;

        // 2. Split the contents by comma to get individual string elements
        var parts = arrayContents.Split(',');

        return parts.Select(part => part.Trim().Trim('"', '\''))
                    .Select(cleanPart => cleanPart.Replace("\\/", "/"))
                    .Aggregate("", (current, cleanPart) => current + cleanPart);
    }

    [GeneratedRegex(@"var u\s*=\s*\[(.*?)\]\.join")]
    private static partial Regex SourceRegex();
}