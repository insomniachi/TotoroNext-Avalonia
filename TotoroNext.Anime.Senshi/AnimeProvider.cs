using System.Runtime.CompilerServices;
using Flurl;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Senshi;

public class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.Request("anime", "filter")
                                   .PostJsonAsync(new
                                   {
                                       searchTerm = query,
                                       page = 1,
                                       limit = 10
                                   }, cancellationToken: ct)
                                   .ReceiveJson<SenshiSearchResponse>();

        foreach (var item in response.Items)
        {
            var image = Url.Combine(client.BaseUrl, item.Image);
            yield return new SearchResult(this, item.Id, item.Title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var info = await client.Request("anime", animeId)
                               .GetJsonAsync<SenshiItem>(cancellationToken: ct);

        var episodes = await client.Request("episodes", info.InternalId)
                                   .GetJsonAsync<List<SenshiEpisode>>(cancellationToken: ct);

        foreach (var episode in episodes)
        {
            yield return new Episode(this, info.InternalId.ToString(),
                                     $"{episode.Episode}",
                                     episode.Episode);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var servers = await client.Request("episode-embeds", animeId, episodeId)
                            .GetJsonAsync<List<SenshiStream>>(cancellationToken: ct);

        foreach (var server in servers)
        {
            yield return new VideoServer(server.Type, new Uri(server.StreamUrl))
            {
                Headers =
                {
                    [HeaderNames.Referer] = client.BaseUrl,
                    [HeaderNames.UserAgent] = Http.UserAgent
                }
            };
        }
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient("Senshi"));
    }
}