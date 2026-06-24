using Downloader;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class StandardDownloader : BaseDownloader
{
    protected override IDownloadOperation CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        var configuration = new DownloadConfiguration { RequestConfiguration = new RequestConfiguration() };
        var builder = DownloadBuilder.New()
                                     .WithUrl(server.Url)
                                     .WithDirectory(Path.GetDirectoryName(filepath))
                                     .WithFileName(Path.GetFileName(filepath))
                                     .WithConfiguration(configuration);

        foreach (var header in server.Headers)
        {
            configuration.RequestConfiguration.Headers.Add(header.Key, header.Value);
        }

        var download = builder.Build();
        var operation = new StandardDownloadOperation(download)
        {
            FileName = filepath,
            Link = new Uri(download.Url)
        };
        
        return operation;
    }
}