namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentStream
{
    Task<Uri?> TryGetStreamUrl(Uri magnet, CancellationToken ct);
}