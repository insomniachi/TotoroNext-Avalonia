using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.Playwright;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen;

public partial class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    private string? _searchToken;
    private readonly string _apiToken = settings.Value.ApiToken;

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        _searchToken ??= await GetSearchToken();
        var response = await "https://search.animeonsen.xyz/indexes/content/search"
                             .WithOAuthBearerToken(_searchToken)
                             .PostJsonAsync(new
                             {
                                 q = query
                             })
                             .ReceiveJson<AnimeOnsenSearchResult>();

        foreach (var item in response.Data)
        {
            var image = $"https://api.animeonsen.xyz/v4/image/210x300/{item.Id}";
            yield return new SearchResult(this, item.Id, item.Title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var response = await $"https://api.animeonsen.xyz/v4/content/{animeId}/video/{episodeId}"
                             .WithOAuthBearerToken(_apiToken)
                             .GetJsonAsync<AnimeOnsenStreamResult>();

        yield return new VideoServer("Default", new Uri(response.Stream.Url))
        {
            Subtitle = response.Stream.Subtitles.English,
            Headers =
            {
                [HeaderNames.Referer] = "https://www.animeonsen.xyz/"
            }
        };
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var response = await $"https://api.animeonsen.xyz/v4/content/{animeId}/episodes"
                             .WithOAuthBearerToken(_apiToken)
                             .GetJsonAsync<Dictionary<string, AnimeOnsenEpisode>>();

        foreach (var item in response)
        {
            if (!float.TryParse(item.Key, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, item.Key, number)
            {
                Info = new EpisodeInfo
                {
                    Titles = new Titles
                    {
                        English = item.Value.TitleEnglish,
                        Japanese = item.Value.TitleJapanese
                    }
                }
            };
        }
    }

    private static async Task<string?> GetSearchToken()
    {
        var content = await "https://www.animeonsen.xyz/".GetStringAsync();
        var match = GetTokenRegex().Match(content);
        return match.Success ? match.Groups["Token"].Value : null;
    }

    [GeneratedRegex("""
                    <meta name="ao-search-token" content="(?<Token>.*)"
                    """)]
    private static partial Regex GetTokenRegex();
}

[UsedImplicitly]
internal class AnimeOnsenSearchResult
{
    [JsonPropertyName("hits")] public IReadOnlyList<AnimeOnsenItemModel> Data { get; init; } = [];
}

[UsedImplicitly]
internal class AnimeOnsenStreamResult
{
    [JsonPropertyName("uri")] public AnimeOnsenStream Stream { get; set; } = new();
}

[UsedImplicitly]
internal class AnimeOnsenItemModel
{
    [JsonPropertyName("content_title")] public string Title { get; set; } = "";
    [JsonPropertyName("content_id")] public string Id { get; set; } = "";
}

[UsedImplicitly]
internal class AnimeOnsenEpisode
{
    [JsonPropertyName("contentTitle_episode_en")]
    public string TitleEnglish { get; set; } = "";

    [JsonPropertyName("contentTitle_episode_jp")]
    public string TitleJapanese { get; set; } = "";
}

internal class AnimeOnsenStream
{
    [JsonPropertyName("stream")] public string Url { get; set; } = "";
    [JsonPropertyName("subtitles")] public AnimeOnsenSubtitles Subtitles { get; set; } = new();
}

internal class AnimeOnsenSubtitles
{
    [JsonPropertyName("en-US")] public string English { get; set; } = "";
}