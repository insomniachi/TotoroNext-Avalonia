using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Torrents.Abstractions.ViewModels;

public partial class TorrentViewModel : ObservableObject
{
    public required string Name { get; init; }

    public required string Hash { get; init; }

    [ObservableProperty] public partial int? Seeders { get; set; }

    [ObservableProperty] public partial int? Leechers { get; set; }

    [ObservableProperty] public partial double? Progress { get; set; }

    [ObservableProperty] public partial long? DownloadSpeed { get; set; }


    public void Update(TorrentViewModel updated)
    {
        if (updated.Seeders is { } seeders)
        {
            Seeders = seeders;
        }

        if (updated.Leechers is { } leechers)
        {
            Leechers = leechers;
        }

        if (updated.Progress is { } progress)
        {
            Progress = progress;
        }

        if (updated.DownloadSpeed is { } downloadSpeed)
        {
            DownloadSpeed = downloadSpeed;
        }
    }
}