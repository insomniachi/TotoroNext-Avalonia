namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentClient
{
    Task AddTorrent(AddTorrentRequest request);
    IAsyncEnumerable<ViewModels.TorrentViewModel> GetTorrents(CancellationToken cancellationToken);
}