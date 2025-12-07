using System.Reactive.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

#pragma warning disable CS9113 // Parameter is unread.

namespace TotoroNext.Anime;

public partial class DownloadOperation(AnimeModel anime, Episode episode, VideoServer server, string filename) : ObservableObject
{
    private IDownload? _operation;
    private DownloadProgressChangedEventArgs? _progress;

    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial bool DownloadStarted { get; set; }
    [ObservableProperty] public partial double Speed { get; set; }
    [ObservableProperty] public partial long DownloadedBytes { get; set; }
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial bool IsCompleted { get; set; }
    [ObservableProperty] public partial bool IsPaused { get; set; }
    [ObservableProperty] public partial bool IsCancelled { get; set; }

    public Uri Link { get; set; } = server.Url;
    public string FileName { get; } = filename;

    public async Task StartAsync()
    {
        var configuration = new DownloadConfiguration { RequestConfiguration = new RequestConfiguration() };
        var builder = DownloadBuilder.New()
                                     .WithUrl(server.Url)
                                     .WithDirectory(FileHelper.GetPath("Downloads"))
                                     .WithFileName(FileName)
                                     .WithConfiguration(configuration);

        foreach (var header in server.Headers)
        {
            configuration.RequestConfiguration.Headers.Add(header.Key, header.Value);
        }

        _operation = builder.Build();

        _operation.DownloadStarted += (_, _) => Dispatcher.UIThread.Invoke(() => DownloadStarted = true);
        _operation.DownloadFileCompleted += (_, _) => Dispatcher.UIThread.Invoke(() =>
        {
            Progress = 100;
            IsCompleted = true;
        });
        _operation.DownloadProgressChanged += (_, e) => { _progress = e; };

        var subscription = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
                                     .Select(_ => _progress)
                                     .WhereNotNull()
                                     .ObserveOn(RxApp.MainThreadScheduler)
                                     .Subscribe(e =>
                                     {
                                         Progress = e.ProgressPercentage;
                                         Speed = e.AverageBytesPerSecondSpeed;
                                         TotalBytes = e.TotalBytesToReceive;
                                         DownloadedBytes = e.ReceivedBytesSize;
                                     });

        await _operation.StartAsync();
        subscription.Dispose();
    }

    [RelayCommand]
    private void TogglePauseResume()
    {
        if (_operation is null)
        {
            return;
        }

        switch (_operation.Status)
        {
            case DownloadStatus.Paused:
                _operation.Resume();
                IsPaused = false;
                break;
            case DownloadStatus.Running:
                _operation.Pause();
                IsPaused = true;
                break;
        }
    }


    [RelayCommand]
    private void Cancel()
    {
        if (_operation is null)
        {
            return;
        }

        _operation.Stop();
        var file = Path.Combine(_operation.Folder, _operation.Filename);
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        IsCompleted = true;
        IsCancelled = true;
    }
}