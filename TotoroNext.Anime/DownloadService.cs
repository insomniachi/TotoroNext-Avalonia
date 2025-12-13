using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime;

[UsedImplicitly]
public class DownloadService(
    IMessenger messenger,
    IDownloadManager downloadManager) : IHostedService, IRecipient<DownloadRequest>
{
    private readonly SemaphoreSlim _semaphore = new(3, 3);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        messenger.Unregister<DownloadRequest>(this);
        return Task.CompletedTask;
    }

    public void Receive(DownloadRequest message)
    {
        _ = Task.Run(() => ProcessDownloadRequest(message));
    }

    private async Task StartDownload(DownloadRequest message)
    {
        var allEpisodes = await message.Provider.GetEpisodes(message.SearchResult.Id, CancellationToken.None).ToListAsync();
        var targetEpisodes = allEpisodes.Where(x => x.Number >= message.EpisodeStart && x.Number <= message.EpisodeEnd).ToList();

        foreach (var episode in targetEpisodes)
        {
            var servers = await episode.GetServersAsync(CancellationToken.None).ToListAsync();
            foreach (var server in servers)
            {
                // can't download hls
                if (server.Url.AbsoluteUri.Contains("m3u8"))
                {
                    continue;
                }

                var operation = new DownloadOperation(message.Anime, episode, server, CreateFilename(message, episode, server));
                downloadManager.AddDownload(operation);
                await operation.StartAsync();
                break;
            }
        }
    }

    private async Task ProcessDownloadRequest(DownloadRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            await StartDownload(request);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string CreateFilename(DownloadRequest message, Episode episode, VideoServer server)
    {
        var directory = message.SaveFolder ?? FileHelper.GetPath("Downloads");
        var absoluteEpNumber = episode.Number + message.EpisodeOffset;

        var fileName = string.IsNullOrEmpty(message.FilenameFormat)
            ? $"{message.Anime.Title} - Episode - {absoluteEpNumber}.{server.ContentType}"
            : $"{message.FilenameFormat.Replace("{ep}", absoluteEpNumber.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'))}.{server.ContentType}";

        return Path.Combine(directory, fileName);
    }
}