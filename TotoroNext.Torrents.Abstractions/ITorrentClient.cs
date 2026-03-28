using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace TotoroNext.Torrents.Abstractions;

public interface ITorrentClient
{
    Task AddTorrent(AddTorrentRequest request);
    IAsyncEnumerable<TorrentViewModel> GetTorrents(CancellationToken cancellationToken);
}

public class AddTorrentRequest
{
    public required string[] Torrents { get; set; }
    public required string SaveDirectory { get; set; }
    public string? Tags { get; set; }
}

public partial class TorrentViewModel : ObservableObject
{
    public required string Name { get; init; }

    public required string Hash { get; init; }

    [ObservableProperty] public partial int Seeders { get; set; }

    [ObservableProperty] public partial int Leechers { get; set; }

    [ObservableProperty] public partial double Progress { get; set; }

    [ObservableProperty] public partial long DownloadSpeed { get; set; }


    public void Update(TorrentViewModel updated)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            Seeders = updated.Seeders;
            Leechers = updated.Leechers;
            Progress = updated.Progress;
            DownloadSpeed = updated.DownloadSpeed;
        });
    }
}