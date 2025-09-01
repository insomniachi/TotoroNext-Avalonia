using System.Collections.ObjectModel;

namespace TotoroNext.Anime;

public interface IDownloadManager
{
    ReadOnlyObservableCollection<DownloadOperation> Downloads { get; }
    void AddDownload(DownloadOperation download);
}