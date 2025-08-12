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
                             .GetJsonAsync<SearchResponseRoot>();

        foreach (var item in response.Data.Items)
        {
            yield return new SearchResult(this, item.Id, item.Title, new Uri(item.PosterImage.Original));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var response = await $"https://www.animeparadise.moe/watch/{episodeId}"
                             .AppendQueryParam("origin", animeId)
                             .WithHeader("next-action", "60553ef556eeb58ac6b7604ec273a67f0202e7dda1")
                             .PostStringAsync($"""["{episodeId}","{animeId}"]""")
                             .ReceiveString();

        var payload = response.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last()["1:".Length..];
        var doc = JsonDocument.Parse(payload);
        var episode = doc.RootElement.GetProperty("episode");
        var url = episode.GetProperty("streamLink").GetString() ?? "";
        var actualUrl = "https://stream.animeparadise.moe/m3u8".AppendQueryParam("url", url).ToUri();
        var subData = episode.GetProperty("subData");
        var skipData = episode.GetProperty("skipData").Deserialize<SkipData>();

        var server = new VideoServer("Default", actualUrl)
        {
            Headers =
            {
                [HeaderNames.Referer] = "https://www.animeparadise.moe/"
            },
            SkipData = GetSkipData(skipData)
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
    
    private static TotoroNext.Anime.Abstractions.Models.SkipData? GetSkipData(SkipData? skipData)
    {
        if (skipData is null)
        {
            return null;
        }

        var data = new TotoroNext.Anime.Abstractions.Models.SkipData();
        
        if (skipData.Intro.End > 0)
        {
            data.Opening = new Segment()
            {
                Start = TimeSpan.FromSeconds(skipData.Intro.Start),
                End = TimeSpan.FromSeconds(skipData.Intro.End)
            };
        }

        if (skipData.Outro.End > 0)
        {
            data.Ending = new Segment()
            {
                Start = TimeSpan.FromSeconds(skipData.Outro.Start),
                End = TimeSpan.FromSeconds(skipData.Outro.End)
            };
        }

        return data;
    }
}

[UsedImplicitly]
internal class SearchResponseRoot
{
    [JsonPropertyName("data")] public SearchResponse Data { get; set; }
}

[UsedImplicitly]
internal class SearchResponse
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("searchData")] public List<AnimeParadiseModel> Items { get; init; } = [];
}

[UsedImplicitly]
internal class EpisodeResponse
{
    [JsonPropertyName("data")] public List<AnimeParadiseEpisode> Data { get; init; } = [];
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

internal class SkipDataItem
{
    [JsonPropertyName("start")] public int Start { get; set; }
    [JsonPropertyName("end")] public int End { get; set; }
}

internal class SkipData
{
    [JsonPropertyName("intro")] public SkipDataItem Intro { get; set; } = new();
    [JsonPropertyName("outro")] public SkipDataItem Outro { get; set; } = new();
}