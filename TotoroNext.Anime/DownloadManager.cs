using System.Collections.ObjectModel;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime;

internal class DownloadManager : IDownloadManager
{
    public const int MaxConcurrentDownloads = 3;
    private readonly ObservableCollection<IDownloadOperation> _downloads = [];
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentDownloads, MaxConcurrentDownloads);

    public DownloadManager()
    {
        Downloads = new ReadOnlyObservableCollection<IDownloadOperation>(_downloads);
    }

    public ReadOnlyObservableCollection<IDownloadOperation> Downloads { get; }

    public void AddDownload(IDownloadOperation download)
    {
        _ = Task.Run(async () =>
        {
            await _semaphore.WaitAsync();
            try
            {
                _downloads.Add(download);
                await download.StartAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        });

    }
}