using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.AnimeGG;

public partial class AnimeProvider : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var stream = await "https://www.animegg.org/search/auto/"
                           .AppendQueryParam("q", query)
                           .GetStreamAsync();

        var node = await JsonNode.ParseAsync(stream);
        var results = node!.Deserialize<List<AnimeGgItem>>() ?? [];
        foreach (var result in results)
        {
            yield return new SearchResult(this, result.Url, result.Name);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var stream = await episodeId
            .GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);
        var servers = doc.QuerySelectorAll("#videos li a") ?? [];

        foreach (var server in servers)
        {
            var id = server.GetAttributeValue("data-id", "");
            var version = server.GetAttributeValue("data-version", "");
            var embed = $"https://www.animegg.org/embed/{id}/";

            try
            {
                stream = await embed
                               .AppendQueryParam("id", id)
                               .GetStreamAsync();
            }
            catch (Exception)
            {
                continue;
            }
            
            doc.Load(stream);
            var scripts = doc
                          .QuerySelectorAll("script")
                          .FirstOrDefault(s => s.InnerText.Contains("videoSources"))?.InnerText ?? "";

            var match = VideoSourcesRegex().Match(scripts);
            var rawArray = match.Groups[1].Value;
            rawArray = QuotesRegex().Replace(rawArray, "\"$1\":"); // Quote keys
            rawArray = rawArray.Replace("'", "\""); // Convert single to double quotes
            var sources = JsonSerializer.Deserialize<List<VideoSource>>(rawArray) ?? [];
            sources.Reverse();

            foreach (var source in sources)
            {
                var uri = Url.Combine("https://www.animegg.org/", source.File);
                yield return new VideoServer($"{version} - {source.Label}", new Uri(uri))
                {
                    Headers =
                    {
                        { HeaderNames.Referer, embed}
                    }
                };
            }
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var stream = await "https://www.animegg.org/"
                           .AppendPathSegment(animeId)
                           .GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);

        var episodes = doc.QuerySelectorAll(".newmanga li") ?? [];

        foreach (var episode in episodes.Reverse())
        {
            var title = episode.QuerySelector(".anititle")?.InnerText ?? "";
            var link = episode.QuerySelector(".anm_det_pop");
            var url = Url.Combine("https://www.animegg.org/", link?.GetAttributeValue("href", ""));
            var content = link?.InnerText ?? "";
            var number = content.Split(" ").LastOrDefault();
            _ = float.TryParse(number, out var episodeNumber);

            yield return new Episode(this, animeId, url, episodeNumber)
            {
                Info = new EpisodeInfo
                {
                    Titles =
                    {
                        English = title
                    }
                }
            };
        }
    }

    [GeneratedRegex(@"videoSources\s*=\s*(\[.*?\]);", RegexOptions.Singleline)]
    private static partial Regex VideoSourcesRegex();
    [GeneratedRegex(@"(\w+):")]
    private static partial Regex QuotesRegex();
}

[Serializable]
internal class AnimeGgItem
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("url")] public string Url { get; set; } = "";

    [JsonPropertyName("thumbnailUrl")] public string Image { get; set; } = "";
}

[Serializable]
public class VideoSource
{
    [JsonPropertyName("file")] public string File { get; set; } = "";

    [JsonPropertyName("label")] public string Label { get; set; } = "";
}