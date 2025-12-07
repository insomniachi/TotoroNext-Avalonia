using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime;

public class TorrentAnimeProvider(IEnumerable<TorrentModel> torrents, ITorrentExtractor torrentExtractor) : IAnimeProvider
{
    public IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        return AsyncEnumerable.Empty<SearchResult>();
    }

    public IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        return AsyncEnumerable.Repeat(new VideoServer("Default", new Uri(episodeId), torrentExtractor), 1);
    }

    public IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        return torrents
               .Select(x => new Episode(this, animeId, x.Torrent.ToString(), x.Episode ?? 0))
               .ToAsyncEnumerable();
    }
}