using System.Runtime.CompilerServices;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class AnimeProvider(IHttpClientFactory httpClientFactory,
                           IModuleSettings<Settings> settings) : IAnimeProvider
{
    public const string BaseUrl = "https://api.tsukihime.org/v1/";

    public async IAsyncEnumerable<SearchResult> SearchAsync(SearchOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("animes", "mal", options.Ids?.MyAnimeList)
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var id = doc.RootElement.GetProperty("id").GetInt32();
        var title = doc.RootElement.GetProperty("title").GetString()!;
        var image = doc.RootElement.GetProperty("thumbnail").GetString()!;

        yield return new SearchResult(this, id.ToString(), title, new Uri(image));
    }

    public IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken ct)
    {
        return AsyncEnumerable.Empty<SearchResult>();
    }
    
    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("animes", animeId, "episodes")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        
        foreach (var id in doc.RootElement.EnumerateArray().Select(episode => episode.GetProperty("id").GetInt32()))
        {
            yield return new Episode(this, animeId, id.ToString(), id);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.Request("animes", animeId, "episodes", episodeId)
                                 .GetJsonAsync<EpisodeTorrentsListResponse>(cancellationToken: ct);

        foreach (var torrent in response.Results.Where(x => x.Id == settings.Value.Group))
        {
            yield return new VideoServer(torrent.Group.Name, new Uri($"https://api.tsukihime.org/v1/torrents/{torrent.Id}"));
        }
    }

    private FlurlClient CreateClient() => new(httpClientFactory.CreateClient($"{Module.Id}-api"));
}