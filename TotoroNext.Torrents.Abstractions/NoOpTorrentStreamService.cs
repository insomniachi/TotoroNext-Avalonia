namespace TotoroNext.Torrents.Abstractions;

public class NoOpTorrentStreamService : ITorrentStream
{
    public Task<Uri?> TryGetStreamUrl(Uri uri, CancellationToken ct)
    {
        return Task.FromResult<Uri?>(uri);
    }
}