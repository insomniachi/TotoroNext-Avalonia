using System.Reactive.Linq;
using Avalonia.Threading;
using Downloader;
using ReactiveUI;

namespace TotoroNext.Anime.Abstractions;

public class StandardDownloadOperation(IDownload download) : BaseDownloadOperation
{
    private DownloadProgressChangedEventArgs? _progress;

    public override async Task StartAsync()
    {
        download.DownloadStarted += (_, _) => Dispatcher.UIThread.Invoke(() => DownloadStarted = true);
        download.DownloadFileCompleted += (_, _) => Dispatcher.UIThread.Invoke(() =>
        {
            Progress = 100;
            IsCompleted = true;
        });
        download.DownloadProgressChanged += (_, e) => { _progress = e; };
        var subscription = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                                     .Select(_ => _progress)
                                     .WhereNotNull()
                                     .ObserveOn(RxSchedulers.MainThreadScheduler)
                                     .Subscribe(e =>
                                     {
                                         Progress = e.ProgressPercentage;
                                         Speed = e.AverageBytesPerSecondSpeed;
                                         TotalBytes = e.TotalBytesToReceive;
                                         DownloadedBytes = e.ReceivedBytesSize;
                                     });

        await download.StartAsync();
        subscription.Dispose();
    }

    protected override void TogglePauseResumeImpl()
    {
        switch (download.Status)
        {
            case DownloadStatus.Paused:
                download.Resume();
                IsPaused = false;
                break;
            case DownloadStatus.Running:
                download.Pause();
                IsPaused = true;
                break;
            case DownloadStatus.None:
            case DownloadStatus.Created:
            case DownloadStatus.Stopped:
            case DownloadStatus.Completed:
            case DownloadStatus.Failed:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void CancelImpl()
    {
        download.Stop();
        var file = Path.Combine(download.Folder, download.Filename);
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        IsCompleted = true;
        IsCancelled = true;
    }
}