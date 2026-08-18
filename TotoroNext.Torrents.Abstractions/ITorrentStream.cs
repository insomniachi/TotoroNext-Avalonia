namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentStream
{
    Task<Uri?> TryGetStreamUrl(Uri torrentUri, CancellationToken ct);
}