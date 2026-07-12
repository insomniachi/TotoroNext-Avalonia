using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeProvider
{
    IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken ct);
    IAsyncEnumerable<SearchResult> SearchAsync(SearchOptions options, CancellationToken ct) => SearchAsync(options.Query, ct);
    IAsyncEnumerable<Episode> GetEpisodes(string animeId, CancellationToken ct);
    IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, CancellationToken ct);

    List<ModuleOptionItem> GetOptions() => [];
    void UpdateOptions(List<ModuleOptionItem> options) { }
}

public interface IAnimeScheduleProvider
{
    Task<DateTimeOffset?> GetNextEpisodeAiringTime(string animeId, CancellationToken ct);
}