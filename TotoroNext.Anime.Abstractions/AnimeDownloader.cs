using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public class AnimeDownloader(IServiceScopeFactory serviceScopeFactory) : IAnimeDownloader
{
    public async Task<IDownloadOperation?> Download(AnimeModel anime, Episode episode, VideoServer server)
    {
        var fileName = CreateFilename(anime, episode, server);
        var source = (await server.Extract(CancellationToken.None).ToListAsync()).First();
        var download = await CreateDownload(anime, episode, source, fileName);
        return download;
    }
    
    public async IAsyncEnumerable<IDownloadOperation> Download(DownloadRequest request)
    {
        var allEpisodes = await request.Provider.GetEpisodes(request.SearchResult.Id, CancellationToken.None).ToListAsync();
        var targetEpisodes = allEpisodes.Where(x => x.Number >= request.EpisodeStart && x.Number <= request.EpisodeEnd).ToList();

        foreach (var episode in targetEpisodes)
        {
            var servers = await episode.GetServersAsync(CancellationToken.None).ToListAsync();
            var defaultServer = servers.FirstOrDefault(x => x.IsDefault);

            if (defaultServer is null)
            {
                foreach (var server in servers)
                {
                    var fileName = CreateFilename(request, episode, server);
                    var source = (await server.Extract(CancellationToken.None).ToListAsync()).First();
                    var download = await CreateDownload(request.Anime, episode, source, fileName);
                    if (download is not null)
                    {
                        yield return download;
                    }

                    break;
                }
            }
            else
            {
                var fileName = CreateFilename(request, episode, defaultServer);
                var source = (await defaultServer.Extract(CancellationToken.None).ToListAsync()).First();
                var download = await CreateDownload(request.Anime, episode, source, fileName);
                if (download is not null)
                {
                    yield return download;
                }
            }
        }
    }

    protected async Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource server, string filepath)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var downloader = scope.ServiceProvider.GetKeyedService<IDownloader>(server.DownloaderType);

        if (downloader is null)
        {
            return null;
        }
        
        var operation = await downloader.CreateDownload(anime, episode, server, filepath);
        return operation;
    }

    private static string CreateFilename(DownloadRequest message, Episode episode, VideoServer server)
    {
        var directory = FileHelper.GetPath("Downloads");
        var absoluteEpNumber = episode.Number + message.EpisodeOffset;
        var invalidChars = Path.GetInvalidFileNameChars();
        var validTitle = new string(message.Anime.Title.Where(c => !invalidChars.Contains(c)).ToArray());
        var fileName = $"{absoluteEpNumber}.{server.ContentType}";
        return Path.Combine(directory, validTitle, fileName);
    }
    
    private static string CreateFilename(AnimeModel anime, Episode episode, VideoServer server)
    {
        var directory = FileHelper.GetPath("Downloads");
        var absoluteEpNumber = episode.Number;
        var invalidChars = Path.GetInvalidFileNameChars();
        var validTitle = new string(anime.Title.Where(c => !invalidChars.Contains(c)).ToArray());
        var fileName = $"{absoluteEpNumber}.{server.ContentType}";
        return Path.Combine(directory, validTitle, fileName);
    }
}