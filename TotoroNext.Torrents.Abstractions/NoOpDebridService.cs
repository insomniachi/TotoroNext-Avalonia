namespace TotoroNext.Torrents.Abstractions;

public class NoOpDebridService : IDebrid
{
    public Task<Uri?> TryGetDirectDownloadLink(Uri magnet)
    {
        return Task.FromResult<Uri?>(magnet);
    }
}