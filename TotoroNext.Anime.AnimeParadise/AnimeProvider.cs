using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeParadise;

public class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    private readonly Settings _settings = settings.Value;

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var response = await "https://api.animeparadise.moe/search"
                             .AppendQueryParam("q", query)
                             .GetJsonAsync<SearchResponse>();

        foreach (var item in response.Data)
        {
            yield return new SearchResult(this, item.Id, item.Title, new Uri(item.PosterImage.Original));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var response = await $"https://www.animeparadise.moe/_next/data/YF6IP0M9Ftup5FCI243ui/en/watch/{episodeId}.json"
                             .AppendQueryParam("origin", animeId)
                             .AppendQueryParam("id", episodeId)
                             .GetStringAsync();

        var doc = JsonDocument.Parse(response);
        var pageProps = doc.RootElement.GetProperty("pageProps");
        var episode = pageProps.GetProperty("episode");
        var url = episode.GetProperty("streamLink").GetString() ?? "";
        var actualUrl = "https://stream.animeparadise.moe/m3u8".AppendQueryParam("url", url).ToUri();
        var subData = episode.GetProperty("subData");

        var server = new VideoServer("Default", actualUrl)
        {
            Headers =
            {
                [HeaderNames.Referer] = "https://www.animeparadise.moe/"
            }
        };

        var englishSubtitle = "";
        foreach (var item in subData.EnumerateArray())
        {
            var src = item.GetProperty("src").GetString() ?? "";
            if (!src.StartsWith("http"))
            {
                continue;
            }

            var label = item.GetProperty("label").GetString() ?? "";
            if (label == "English")
            {
                englishSubtitle = src;
            }

            if (label == _settings.SubtitleLanguage)
            {
                server.Subtitle = src;
            }
        }

        server.Subtitle ??= englishSubtitle;


        yield return server;
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var response = await $"https://api.animeparadise.moe/anime/{animeId}/episode"
            .GetJsonAsync<EpisodeResponse>();

        foreach (var item in response.Data)
        {
            if (!float.TryParse(item.Number, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, item.Id, number);
        }
    }
}

[UsedImplicitly]
internal class SearchResponse
{
    [JsonPropertyName("data")] public IReadOnlyList<AnimeParadiseModel> Data { get; init; } = [];
}

[UsedImplicitly]
internal class EpisodeResponse
{
    [JsonPropertyName("data")] public IReadOnlyList<AnimeParadiseEpisode> Data { get; init; } = [];
}

[UsedImplicitly]
internal class AnimeParadiseModel
{
    [JsonPropertyName("_id")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("posterImage")] public ImageModel PosterImage { get; init; } = new();
}

internal class ImageModel
{
    [JsonPropertyName("tiny")] public string Tiny { get; init; } = "";
    [JsonPropertyName("medium")] public string Medium { get; init; } = "";
    [JsonPropertyName("large")] public string Large { get; init; } = "";
    [JsonPropertyName("original")] public string Original { get; init; } = "";
}

[UsedImplicitly]
internal class AnimeParadiseEpisode
{
    [JsonPropertyName("uid")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("image")] public string PosterImage { get; init; } = "";
    [JsonPropertyName("number")] public string Number { get; set; } = "";
}