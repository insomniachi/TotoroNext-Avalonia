using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeProvider
{
    IAsyncEnumerable<SearchResult> SearchAsync(string query);
    IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId);
    IAsyncEnumerable<Episode> GetEpisodes(string animeId);

    List<ModuleOptionItem> GetOptions()
    {
        return [];
    }

    void UpdateOptions(List<ModuleOptionItem> options) { }
}