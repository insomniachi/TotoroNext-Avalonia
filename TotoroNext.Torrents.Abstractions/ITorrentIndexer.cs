namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentIndexer
{
    IAsyncEnumerable<TorrentModel> SearchAsync(string query, string groupName, string quality);
}