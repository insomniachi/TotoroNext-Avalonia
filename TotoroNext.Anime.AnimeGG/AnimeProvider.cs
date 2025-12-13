using System.Runtime.CompilerServices;
using System.Text.Json;
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

public partial class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = GetClient();
        var stream = await client
                           .Request("/search/auto/")
                           .AppendQueryParam("q", query)
                           .GetStreamAsync(cancellationToken: ct);

        var results = await JsonSerializer.DeserializeAsync<List<AnimeGgItem>>(stream, cancellationToken: ct);
        foreach (var result in results ?? [])
        {
            ct.ThrowIfCancellationRequested();
            yield return new SearchResult(this, result.Url, result.Name);
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = GetClient();
        var stream = await client.Request(animeId).GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);

        var episodes = doc.QuerySelectorAll(".newmanga li") ?? [];

        foreach (var episode in episodes.Reverse())
        {
            ct.ThrowIfCancellationRequested();
            
            var title = episode.QuerySelector(".anititle")?.InnerText ?? "";
            var link = episode.QuerySelector(".anm_det_pop");
            var id = link?.GetAttributeValue("href", "") ?? "";
            var content = link?.InnerText ?? "";
            var number = content.Split(" ").LastOrDefault();
            _ = float.TryParse(number, out var episodeNumber);

            yield return new Episode(this, animeId, id, episodeNumber)
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

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = GetClient();
        var stream = await client.Request(episodeId).GetStreamAsync(cancellationToken: ct);

        var doc = new HtmlDocument();
        doc.Load(stream);
        var servers = doc.QuerySelectorAll("#videos li a") ?? [];

        foreach (var server in servers)
        {
            ct.ThrowIfCancellationRequested();
            
            var id = server.GetAttributeValue("data-id", "");
            var version = server.GetAttributeValue("data-version", "");
            var embed = Url.Combine(client.BaseUrl, $"/embed/{id}");
            try
            {
                stream = await embed
                               .AppendQueryParam("id", id)
                               .GetStreamAsync(cancellationToken: ct);
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
                ct.ThrowIfCancellationRequested();
                
                var uri = Url.Combine(client.BaseUrl, source.File);
                yield return new VideoServer($"{version} - {source.Label}", new Uri(uri))
                {
                    Headers =
                    {
                        { HeaderNames.Referer, embed }
                    }
                };
            }
        }
    }
    
    [GeneratedRegex(@"videoSources\s*=\s*(\[.*?\]);", RegexOptions.Singleline)]
    private static partial Regex VideoSourcesRegex();

    [GeneratedRegex(@"(\w+):")]
    private static partial Regex QuotesRegex();

    private FlurlClient GetClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient("animegg"));
    }
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