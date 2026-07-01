using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public abstract class BaseDownloader : IAnimeDownloader
{
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
                    var download = CreateDownload(request.Anime, episode, server, fileName);
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
                var download = CreateDownload(request.Anime, episode, defaultServer, fileName);
                if (download is not null)
                {
                    yield return download;
                }
            }
        }
    }

    protected abstract IDownloadOperation? CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath);

    private static string CreateFilename(DownloadRequest message, Episode episode, VideoServer server)
    {
        var directory = FileHelper.GetPath("Downloads");
        var absoluteEpNumber = episode.Number + message.EpisodeOffset;
        var invalidChars = Path.GetInvalidFileNameChars();
        var validTitle = new string(message.Anime.Title.Where(c => !invalidChars.Contains(c)).ToArray());
        var fileName = $"{absoluteEpNumber}.{server.ContentType}";
        return Path.Combine(directory, validTitle, fileName);
    }
}