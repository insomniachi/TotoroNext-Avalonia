using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anidb;

internal partial class AnimeProvider(
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings) : AnimeProvider<Settings>(settings)
{
    public const string BaseUrl = "https://anidb.app";

    public override async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("/browse")
                                 .AppendQueryParam("q", query)
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var node in doc.QuerySelectorAll(".anime-card"))
        {
            var url = node.GetAttributeValue("href", string.Empty);
            var title = node.GetAttributeValue("title", string.Empty);
            var image = node.QuerySelector("img")?.GetAttributeValue("src", string.Empty) ?? string.Empty;

            yield return new SearchResult(this, url, title, new Uri(image));
        }
    }

    public override async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var id = animeId.Split('-').LastOrDefault();

        if (string.IsNullOrEmpty(id))
        {
            yield break;
        }

        using var client = CreateClient();
        var stream = await client.Request($"/api/frontend/anime/{id}/episodes")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        int? firstEp = null;
        foreach (var node in doc.RootElement.GetProperty("episodes").EnumerateArray())
        {
            var epId = node.GetProperty("id").GetInt32();
            var number = node.GetProperty("number").GetInt32();
            firstEp ??= number;
            number = number - firstEp.Value + 1;
            yield return new Episode(this, animeId, epId.ToString(), number);
        }
    }

    public override async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId,
                                                                        [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request($"/api/frontend/episode/{episodeId}/languages")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        foreach (var node in doc.RootElement.GetProperty("languages").EnumerateArray())
        {
            var name = node.GetProperty("name").GetString();
            var embed = node.GetProperty("embed_url").GetString();

            var embedContent = await client.Request(embed).GetStringAsync(cancellationToken: ct);
            if (SourceUrlRegex().Match(embedContent) is not { Success: true } match)
            {
                continue;
            }

            var streamUrl = match.Groups[1].Value;
            yield return new VideoServer(name ?? "", new Uri(streamUrl))
            {
                Headers =
                {
                    [HeaderNames.Referer] = BaseUrl
                },
                IsDefault = Settings.AudioLanguage == name
            };
        }
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}"));
    }

    [GeneratedRegex(@"file:\s*'([^']+)'")]
    private static partial Regex SourceUrlRegex();
}