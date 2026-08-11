using System.Runtime.CompilerServices;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miwayomi;

public class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await GetBaseUrl().AppendPathSegment("search")
                                       .AppendQueryParam("query", query)
                                       .AppendQueryParam("page", 1)
                                       .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var item in doc.RootElement.GetProperty("animes").Deserialize<List<MiwayomiAnime>>() ?? [])
        {
            yield return new SearchResult(this,
                                          item.Url,
                                          item.Title,
                                          item.ThumbnailUrl is null ? null : new Uri(item.ThumbnailUrl));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await GetBaseUrl().AppendPathSegment("episodes")
                                       .AppendQueryParam("url", animeId)
                                       .GetStreamAsync(cancellationToken: ct);

        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var episodes = doc.RootElement.GetProperty("episodes").Deserialize<List<MiwayomiEpisode>>() ?? [];

        foreach (var item in episodes.OrderBy(x => x.EpisodeNumber))
        {
            yield return new Episode(this, animeId, item.Url, item.EpisodeNumber);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = await GetBaseUrl().AppendPathSegment("videos")
                                       .AppendQueryParam("url", episodeId)
                                       .GetStreamAsync(cancellationToken: ct);

        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var item in doc.RootElement.GetProperty("videos").EnumerateArray())
        {
            var url = item.GetProperty("videoUrl").GetString()!;
            var name = item.GetProperty("videoTitle").GetString()!;
            var server = new VideoServer(name, new Uri(url));

            foreach (var header in item.GetProperty("headers").EnumerateObject())
            {
                server.Headers.Add(header.Name, header.Value.GetString()!);
            }

            foreach (var subtitle in from subtitle in item.GetProperty("subtitleTracks").EnumerateArray()
                                     let language = subtitle.GetProperty("lang").GetString()!
                                     where language == "English"
                                     select subtitle)
            {
                server.Subtitle = subtitle.GetProperty("url").GetString()!;
                break;
            }

            yield return server;
        }
    }

    private string GetBaseUrl()
    {
        return $"{settings.Value.BaseUrl}/api/v1/anime/{settings.Value.SelectedSource}";
    }
}