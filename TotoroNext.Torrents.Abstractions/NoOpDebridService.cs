namespace TotoroNext.Torrents.Abstractions;

public class NoOpDebridService : IDebrid
{
    public Task<Uri?> TryGetDirectDownloadLink(Uri magnet, CancellationToken ct)
    {
        return Task.FromResult<Uri?>(magnet);
    }
}