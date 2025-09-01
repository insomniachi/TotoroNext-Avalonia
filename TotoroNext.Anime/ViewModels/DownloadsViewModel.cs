using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public class DownloadsViewModel(IDownloadManager downloadsManager) : ObservableObject
{
    public ReadOnlyObservableCollection<DownloadOperation> Downloads { get; } = downloadsManager.Downloads;
}