using Downloader;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Downloading;

public class StandardDownloader : IDownloader
{
    public async Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        var source = (await server.Extract(CancellationToken.None).ToListAsync()).First();
        var configuration = new DownloadConfiguration { RequestConfiguration = new RequestConfiguration() };
        var builder = DownloadBuilder.New()
                                     .WithUrl(source.Url)
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