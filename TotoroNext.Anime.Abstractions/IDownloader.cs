using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IDownloader
{
    Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource source, string filepath);
}

public static class DownloaderTypes
{
    public const string Http = "http";
    public const string Ytdlp = "ytdlp";
    public const string Torrent = "torrent";
}