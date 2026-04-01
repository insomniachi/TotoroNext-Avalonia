using TotoroNext.Torrents.Abstractions.Models;

namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentIndexer
{
    IAsyncEnumerable<AnimeTorrentModel> SearchAsync(string query, string groupName, string quality);
}