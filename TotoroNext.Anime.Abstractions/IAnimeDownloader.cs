using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeDownloader
{
    public IAsyncEnumerable<IDownloadOperation> Download(DownloadRequest request);
}

public static class DownloaderTypes
{
    public const string Http = "http";
    public const string Ytdlp = "ytdlp";
    public const string Torrent = "torrent";
}