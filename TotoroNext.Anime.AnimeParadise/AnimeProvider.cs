using System.Text.Json;
using Flurl;
using Flurl.Http;
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
                             .WithHeader("next-action", "60c9f65b91846ebfe54b8b0e10169ad4b80073404a")
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

    public List<ModuleOptionItem> GetOptions()
    {
        return settings.Value.ToModuleOptions();
    }

    public void UpdateOptions(List<ModuleOptionItem> options)
    {
        settings.Value.UpdateValues(options);
    }

    private static Abstractions.Models.SkipData? GetSkipData(SkipData? skipData)
    {
        if (skipData is null)
        {
            return null;
        }

        var data = new Abstractions.Models.SkipData();

        if (skipData.Intro.End > 0)
        {
            data.Opening = new Segment
            {
                Start = TimeSpan.FromSeconds(skipData.Intro.Start),
                End = TimeSpan.FromSeconds(skipData.Intro.End)
            };
        }

        if (skipData.Outro.End > 0)
        {
            data.Ending = new Segment
            {
                Start = TimeSpan.FromSeconds(skipData.Outro.Start),
                End = TimeSpan.FromSeconds(skipData.Outro.End)
            };
        }

        return data;
    }
}