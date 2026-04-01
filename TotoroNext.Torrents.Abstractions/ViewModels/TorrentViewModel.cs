using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Torrents.Abstractions.ViewModels;

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
        Seeders = updated.Seeders;
        Leechers = updated.Leechers;
        Progress = updated.Progress;
        DownloadSpeed = updated.DownloadSpeed;
    }
}