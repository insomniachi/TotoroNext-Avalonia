namespace TotoroNext.Torrents.Abstractions;

public interface IDebrid
{
    Task<Uri?> TryGetDirectDownloadLink(Uri magnet);
}