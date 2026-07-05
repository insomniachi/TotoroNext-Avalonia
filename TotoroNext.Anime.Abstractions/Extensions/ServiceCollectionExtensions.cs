using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDebrid()
        {
            return services.AddTransient<ITorrentExtractor, TorrentExtractor>()
                           .AddKeyedTransient<ITorrentStream, NoOpTorrentStreamService>(Guid.Empty)
                           .AddKeyedTransient<ITorrentStream, MonoTorrentStream>(MonoTorrentStream.MonoTorrentStreamId);
        }

        public IServiceCollection AddDownloaders()
        {
            return services.AddTransient<IAnimeDownloader, AnimeDownloader>()
                           .AddKeyedTransient<IDownloader, StandardDownloader>(DownloaderTypes.Http)
                           .AddKeyedTransient<IDownloader, YtdlpDownloader>(DownloaderTypes.Ytdlp);
        }
    }
}