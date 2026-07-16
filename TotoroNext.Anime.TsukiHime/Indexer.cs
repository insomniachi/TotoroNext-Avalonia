using Flurl.Http;
using TotoroNext.Torrents.Abstractions;
using TotoroNext.Torrents.Abstractions.Models;

namespace TotoroNext.Anime.TsukiHime;

public class Indexer(IHttpClientFactory clientFactory) : ITorrentIndexer
{
    public async IAsyncEnumerable<AnimeTorrentModel> SearchAsync(TorrentSearchOptions options)
    {
        if (options.MyAnimeListId is null || string.IsNullOrEmpty(options.GroupName))
        {
            yield break;
        }
        
        var groupDescriptor = TsukiHimeLocalData.Groups.FirstOrDefault(x => x.Name == options.GroupName);
        if (groupDescriptor is null)
        {
            yield break;
        }

        using var client = new FlurlClient(clientFactory.CreateClient($"{Module.Id}"));
        var animeResponse = await client.Request("animes", "mal", options.MyAnimeListId)
                                        .GetJsonAsync<AnimeDescriptor>();

        var torrents = await client.Request("groups", groupDescriptor.Id, "animes", animeResponse.Id)
                                   .GetJsonAsync<EpisodeTorrentsListResponse>();

        
        foreach (var torrent in torrents.Results)
        {
            var info = TorrentInfo.Parse(torrent);

            if (!string.IsNullOrEmpty(info.Resolution) &&
                !string.IsNullOrEmpty(options.Quality) &&
                !info.Resolution.Contains(options.Quality))
            {
                continue;
            }
            
            yield return new AnimeTorrentModel()
            {
                Title = torrent.Name,
                ReleaseGroup = options.GroupName,
                Torrent = new Uri($"https://nyaa.si/download/{torrent.NyaaId}.torrent")
            };
        }
    }

    public IEnumerable<string> GetReleaseGroups() => TsukiHimeLocalData.Groups.Select(x => x.Name);
}