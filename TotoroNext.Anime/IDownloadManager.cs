using System.Collections.ObjectModel;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime;

public interface IDownloadManager
{
    ReadOnlyObservableCollection<IDownloadOperation> Downloads { get; }
    void AddDownload(IDownloadOperation download);
}