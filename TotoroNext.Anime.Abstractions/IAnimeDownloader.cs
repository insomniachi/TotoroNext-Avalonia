using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeDownloader
{
    IAsyncEnumerable<IDownloadOperation> Download(DownloadRequest request);
}