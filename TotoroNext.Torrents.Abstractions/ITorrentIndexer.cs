using TotoroNext.Torrents.Abstractions.Models;

namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentIndexer
{
    IAsyncEnumerable<AnimeTorrentModel> SearchAsync(TorrentSearchOptions options);
    IEnumerable<string> GetReleaseGroups();
}

public class TorrentSearchOptions
{
    public long? MyAnimeListId { get; set; }
    public long? AnilistId { get; set; }
    public string? Query { get; set; }
    public string? GroupName { get; set; }
    public string? Quality { get; set; }
}