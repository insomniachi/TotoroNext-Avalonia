namespace TotoroNext.Torrents.Abstractions;

public class NoOpDebridService : IDebrid
{
    public Task<Uri?> TryGetDirectDownloadLink(Uri magnet) => Task.FromResult<Uri?>(magnet);
}