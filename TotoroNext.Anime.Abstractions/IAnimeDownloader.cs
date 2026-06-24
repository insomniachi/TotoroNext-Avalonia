using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeDownloader
{
    public IAsyncEnumerable<IDownloadOperation> Download(DownloadRequest request);
}