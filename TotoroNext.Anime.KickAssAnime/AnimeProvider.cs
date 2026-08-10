using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.KickAssAnime;

public partial class AnimeProvider(
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings) : IAnimeProvider
{
    public const string BaseUrl = "https://kaa.lt/";

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("/api/search")
                                 .PostJsonAsync(new { query }, cancellationToken: ct)
                                 .ReceiveStream();
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var id = item.GetProperty("slug").GetString()!;
            var title = item.GetProperty("title").GetString()!;
            var image = Url.Combine(BaseUrl, "/image/poster/", $"{item.GetProperty("poster").GetProperty("hq").GetString()}.webp")!;

            yield return new SearchResult(this, id, title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request($"/api/show/{animeId}/language")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var languages = doc.RootElement.GetProperty("result").EnumerateArray().Select(x => x.GetString()).ToList();

        if (!languages.Contains(settings.Value.AudioLanguage))
        {
            yield break;
        }

        var page = 1;
        int? pageCount = null;

        do
        {
            ct.ThrowIfCancellationRequested();
            stream = await client.Request($"/api/show/{animeId}/episodes")
                                 .AppendQueryParam("page", page)
                                 .AppendQueryParam("lang", settings.Value.AudioLanguage)
                                 .GetStreamAsync(cancellationToken: ct);
            doc.Dispose();
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                yield return ParseEpisode(item, animeId);
            }

            pageCount ??= doc.RootElement.GetProperty("pages").EnumerateArray().Count();
            page++;
        } while (page <= pageCount);
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request($"/api/show/{animeId}/episode/{episodeId}")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var item in doc.RootElement.GetProperty("servers").EnumerateArray())
        {
            var title = item.GetProperty("name").GetString()!;
            var embed = item.GetProperty("src").GetString()!;

            if (embed.Contains("/vast", StringComparison.InvariantCultureIgnoreCase))
            {
                embed = ReplacePath(embed, "/cat-player/player");
            }
            
            var response = await client.Request(embed).GetStringAsync(cancellationToken: ct);
            var html = WebUtility.HtmlDecode(response).Replace("&quot;", "\"");

            if (ManifestRegex().Match(html) is not { Success: true } match)
            {
                continue;
            }
            
            if(TracksRegex().Matches(html) is not { Count: > 0 } trackMatches)
            {
                continue;
            }
            
            var selectedTrack = trackMatches.FirstOrDefault(x => x.Groups[2].Value == settings.Value.SubtitleLanguage);

            var json = $$"""{ {{match.Groups[0].Value}}] }""";
            var manifestUrl = JsonDocument.Parse(json).RootElement.GetProperty("manifest").EnumerateArray().ElementAt(1).GetString()!;
            var origin = Uri.TryCreate(embed, UriKind.Absolute, out var uri)
                ? uri.GetLeftPart(UriPartial.Authority)
                : BaseUrl;
            
            yield return new VideoServer(title, new Uri(FixUrl(manifestUrl, embed)!))
            {
                Subtitle = selectedTrack?.Groups[3].Value,
                Headers =
                {
                    ["Origin"] = origin
                }
            };
        }
    }

    public List<ModuleOptionItem> GetOptions()
    {
        return settings.Value.ToModuleOptions();
    }

    public void UpdateOptions(List<ModuleOptionItem> options)
    {
        settings.Value.UpdateValues(options);
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}"));
    }

    private Episode ParseEpisode(JsonElement item, string animeId)
    {
        var id = $"ep-{item.GetProperty("episode_string").GetString()}-{item.GetProperty("slug").GetString()}";
        var number = item.GetProperty("episode_number").GetInt16();
        var image = Url.Combine(BaseUrl, "/image/poster/", $"{item.GetProperty("thumbnail").GetProperty("hq").GetString()}.webp")!;
        var title = item.GetProperty("title").GetString()!;

        return new Episode(this, animeId, id, number)
        {
            Info = new EpisodeInfo
            {
                Titles = new Titles
                {
                    English = title
                },
                Image = image
            }
        };
    }
    
    private static string ReplacePath(string url, string path)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var builder = new UriBuilder(uri) { Path = path, Query = uri.Query.TrimStart('?') };
        return builder.Uri.ToString();
    }
    
    private static string? FixUrl(string? rawUrl, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        var trimmed = WebUtility.HtmlDecode(rawUrl).Replace("\\/", "/").Trim();
        if (trimmed.StartsWith(@"http://") || trimmed.StartsWith("https://"))
            return HttpSlashRegex().Replace(trimmed, "$1//");

        if (trimmed.StartsWith("//"))
        {
            return "https://" + trimmed.TrimStart('/');
        }

        if (trimmed.StartsWith('/') && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return uri.GetLeftPart(UriPartial.Authority) + trimmed;
        }

        return trimmed;
    }

    [GeneratedRegex("\"manifest\":\\[0,\"(?:https?:)?(?<url>//[^\"]+)\"")]
    private static partial Regex ManifestRegex();

    [GeneratedRegex( "\"language\":\\[\\d+,\"(?<language>[^\"]+)\"\\][^}]+?\"name\":\\[\\d+,\"(?<name>[^\"]+)\"\\][^}]+?\"src\":\\[\\d+,\"(?<src>[^\"]+)\"\\]")]
    private static partial Regex TracksRegex();

    [GeneratedRegex(@"^(https?:)//+")]
    private static partial Regex HttpSlashRegex();
}