using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public class DownloadsViewModel(IDownloadService downloadsService) : ObservableObject
{
    public ObservableCollection<DownloadOperation> Downloads { get; } = downloadsService.Downloads;
}