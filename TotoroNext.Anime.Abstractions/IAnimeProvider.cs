using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeProvider
{
    IAsyncEnumerable<SearchResult> SearchAsync(string query);
    IAsyncEnumerable<Episode> GetEpisodes(string animeId);
    IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId);

    List<ModuleOptionItem> GetOptions() => [];
    void UpdateOptions(List<ModuleOptionItem> options) { }
}