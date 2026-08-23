using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeProvider
{
    IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken ct);
    IAsyncEnumerable<SearchResult> SearchAsync(SearchOptions options, CancellationToken ct) => SearchAsync(options.Query, ct);
    IAsyncEnumerable<Episode> GetEpisodes(string animeId, CancellationToken ct);
    IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, CancellationToken ct);
    DataContainer GetOptions() => [];
    void UpdateOptions(List<DataContainerProperty> options) { }
}

public interface IAnimeScheduleProvider
{
    Task<DateTimeOffset?> GetNextEpisodeAiringTime(string animeId, CancellationToken ct);
}

public abstract class AnimeProvider<T>(IModuleSettings<T> settings) : IAnimeProvider
    where T : OverridableConfig, new()
{
    protected T Settings => settings.Value;
    public abstract IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken ct);
    public abstract IAsyncEnumerable<Episode> GetEpisodes(string animeId, CancellationToken ct);
    public abstract IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, CancellationToken ct);
    public virtual IAsyncEnumerable<SearchResult> SearchAsync(SearchOptions options, CancellationToken ct) => SearchAsync(options.Query, ct);

    public DataContainer GetOptions() => settings.Value.ToDataContainer();
    public void UpdateOptions(List<DataContainerProperty> options) => settings.Value.UpdateValues(options);
}