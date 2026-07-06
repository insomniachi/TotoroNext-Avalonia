using System.Collections.ObjectModel;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime;

internal class DownloadManager : IDownloadManager
{
    public const int MaxConcurrentDownloads = 3;
    private readonly ObservableCollection<IDownloadOperation> _downloads = [];
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentDownloads, MaxConcurrentDownloads);
    public event EventHandler? DownloadsChanged;

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
                download.Completed += OnCompleted;
                download.Started += OnStarted;
                await StartDownload(download);
            }
            finally
            {
                _semaphore.Release();
            }
        });

    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        if (sender is not IDownloadOperation download)
        {
            return;
        }
        
        download.Completed -= OnCompleted;
        OnDownloadsChanged();
    }
    
    private void OnStarted(object? sender, EventArgs e)
    {
        if (sender is not IDownloadOperation download)
        {
            return;
        }
        
        download.Started -= OnStarted;
        OnDownloadsChanged();
    }

    private async Task StartDownload(IDownloadOperation download)
    {
        _downloads.Add(download);
        await download.StartAsync();
    }
    
    private void OnDownloadsChanged() => DownloadsChanged?.Invoke(this, EventArgs.Empty);
}