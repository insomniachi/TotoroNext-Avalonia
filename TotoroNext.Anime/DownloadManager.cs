using System.Collections.ObjectModel;

namespace TotoroNext.Anime;

internal class DownloadManager : IDownloadManager
{
    private readonly ObservableCollection<DownloadOperation> _downloads = [];

    public DownloadManager()
    {
        Downloads = new ReadOnlyObservableCollection<DownloadOperation>(_downloads);
    }

    public ReadOnlyObservableCollection<DownloadOperation> Downloads { get; }

    public void AddDownload(DownloadOperation download)
    {
        _downloads.Add(download);
    }
}