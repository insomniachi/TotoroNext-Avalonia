using Downloader;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Downloading;

public class StandardDownloader : IDownloader
{
    public Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource source, string filepath)
    {
        try
        {
            var configuration = new DownloadConfiguration { RequestConfiguration = new RequestConfiguration() };
            var builder = DownloadBuilder.New()
                                         .WithUrl(source.Url)
                                         .WithDirectory(Path.GetDirectoryName(filepath))
                                         .WithFileName(Path.GetFileName(filepath))
                                         .WithConfiguration(configuration);

            foreach (var header in source.Headers)
            {
                configuration.RequestConfiguration.Headers.Add(header.Key, header.Value);
            }

            var download = builder.Build();
            var operation = new StandardDownloadOperation(download, anime, episode)
            {
                FileName = filepath,
                Link = new Uri(download.Url)
            };

            return Task.FromResult<IDownloadOperation?>(operation);
        }
        catch (Exception exception)
        {
            return Task.FromException<IDownloadOperation?>(exception);
        }
    }
}