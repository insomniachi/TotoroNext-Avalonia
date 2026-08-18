namespace TotoroNext.Torrents.Abstractions;

public class NoOpTorrentStreamService : ITorrentStream
{
    public Task<Uri?> TryGetStreamUrl(Uri torrentUri, CancellationToken ct)
    {
        return Task.FromResult<Uri?>(torrentUri);
    }
}