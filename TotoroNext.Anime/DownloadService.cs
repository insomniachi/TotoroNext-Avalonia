using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Downloader;
using Humanizer;
using Humanizer.Bytes;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime;

public interface IDownloadService : IHostedService
{
    ObservableCollection<DownloadOperation> Downloads { get; }
}

[UsedImplicitly]
public class DownloadService(IMessenger messenger) : IDownloadService, IRecipient<DownloadRequest>
{
    public ObservableCollection<DownloadOperation> Downloads { get; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        messenger.Unregister<DownloadRequest>(this);
        return Task.CompletedTask;
    }

    public void Receive(DownloadRequest message)
    {
        Task.Run(() => StartDownload(message));
    }

    public async Task StartDownload(DownloadRequest message)
    {
        var allEpisodes = await message.Provider.GetEpisodes(message.SearchResult.Id).ToListAsync();
        var targetEpisodes = allEpisodes.Where(x => x.Number >= message.EpisodeStart && x.Number <= message.EpisodeEnd).ToList();

        foreach (var episode in targetEpisodes)
        {
            var servers = await episode.GetServersAsync().ToListAsync();
            foreach (var server in servers)
            {
                // can't download hls
                if (server.Url.AbsoluteUri.Contains("m3u8"))
                {
                    continue;
                }

                var operation = new DownloadOperation(message.Anime, episode, server);
                _ = operation.StartAsync();
                Downloads.Add(operation);
                break;
            }
        }
    }
}

public partial class DownloadOperation(AnimeModel anime, Episode episode, VideoServer server) : ObservableObject
{
    private DownloadProgressChangedEventArgs? _progress;

    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial bool DownloadStarted { get; set; }
    [ObservableProperty] public partial double Speed { get; set; }
    [ObservableProperty] public partial long DownloadedBytes { get; set; }
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial bool IsCompleted { get; set; }

    public Uri Link { get; set; } = server.Url;
    public string FileName { get; set; } = $"{anime.Title} - Episode {episode.Number}";

    public async Task StartAsync()
    {
        var configuration = new DownloadConfiguration { RequestConfiguration = new RequestConfiguration() };
        var builder = DownloadBuilder.New()
                                     .WithUrl(server.Url)
                                     .WithDirectory(ModuleHelper.GetFilePath(null, "Downloads"))
                                     .WithFileName(FileName)
                                     .WithConfiguration(configuration);

        foreach (var header in server.Headers)
        {
            configuration.RequestConfiguration.Headers.Add(header.Key, header.Value);
        }

        var operation = builder.Build();

        operation.DownloadStarted += (_, _) => Dispatcher.UIThread.Invoke(() => DownloadStarted = true);
        operation.DownloadFileCompleted += (_, _) => Dispatcher.UIThread.Invoke(() =>
        {
            Progress = 100;
            IsCompleted = true;
        });
        operation.DownloadProgressChanged += (_, e) =>
        {
            _progress = e;
        };

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

        await operation.StartAsync();
        subscription.Dispose();
    }
}