using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeProvider
{
    IAsyncEnumerable<SearchResult> SearchAsync(string query);
    IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId);
    IAsyncEnumerable<Episode> GetEpisodes(string animeId);
}
