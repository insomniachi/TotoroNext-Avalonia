using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public class DownloadsViewModel(IDownloadManager downloadsManager) : ObservableObject
{
    public ReadOnlyObservableCollection<IDownloadOperation> Downloads { get; } = downloadsManager.Downloads;
}