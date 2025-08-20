using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;
using FuzzySharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.SubsPlease;

public class AnimeProvider : IAnimeProvider
{
    public IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        return Catalog.Items
                      .Where(x => Fuzz.Ratio(query, x.Title) > 50)
                      .Select(x => new SearchResult(this, x.Id, x.Title)).ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var episodes = await GetEpisodesNode(animeId);

        if (!episodes.HasValue)
        {
            yield break;
        }

        var episodeNode = episodes.Value.GetProperty(episodeId);
        var model = episodeNode.Deserialize<SubsPleaseEpisode>();

        if (model is null)
        {
            yield break;
        }

        var items = model.Downloads
                         .OrderByDescending(x => !int.TryParse(x.Resolution, out var resolution) ? int.MinValue : resolution);

        foreach (var resolution in items)
        {
            yield return new VideoServer(resolution.Resolution, new Uri(resolution.Magnet));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var episodes = await GetEpisodesNode(animeId);

        if (!episodes.HasValue)
        {
            yield break;
        }

        foreach (var prop in episodes.Value.EnumerateObject().Reverse())
        {
            var episode = prop.Value.Deserialize<SubsPleaseEpisode>();

            if (episode is null)
            {
                continue;
            }

            if (!float.TryParse(episode.Episode, out var ep))
            {
                continue;
            }

            yield return new Episode(this, animeId, prop.Name, ep);
        }
    }

    private async Task<JsonElement?> GetEpisodesNode(string animeId)
    {
        var stream = await $"https://subsplease.org/shows/{animeId}".GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var table = doc.QuerySelector("#show-release-table");

        if (table is null)
        {
            return null;
        }

        var id = table.GetAttributeValue("sid", "");

        var response = await "https://subsplease.org/api"
                             .AppendQueryParam("f", "show")
                             .AppendQueryParam("tz", TimeZoneInfo.Local.Id)
                             .AppendQueryParam("sid", id)
                             .GetStreamAsync();

        var jsonDoc = await JsonDocument.ParseAsync(response);
        return jsonDoc.RootElement.GetProperty("episode");
    }
}

[Serializable]
internal class SubsPleaseEpisode
{
    [JsonPropertyName("release_date")] public string Date { get; set; } = "";
    [JsonPropertyName("episode")] public string Episode { get; set; } = "";
    [JsonPropertyName("downloads")] public List<SubsPleaseEpisodeDownloadLinks> Downloads { get; set; } = [];
}

[Serializable]
internal class SubsPleaseEpisodeDownloadLinks
{
    [JsonPropertyName("res")] public string Resolution { get; set; } = "";
    [JsonPropertyName("torrent")] public string Torrent { get; set; } = "";
    [JsonPropertyName("magnet")] public string Magnet { get; set; } = "";
    [JsonPropertyName("xdcc")] public string Xdcc { get; set; } = "";
}