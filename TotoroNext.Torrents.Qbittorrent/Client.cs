using System.Runtime.CompilerServices;
using Banned.Qbittorrent;
using Banned.Qbittorrent.Models.Sync;
using Banned.Qbittorrent.Models.Torrent;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using AddTorrentRequest = TotoroNext.Torrents.Abstractions.AddTorrentRequest;
using QbAddTorrentRequest = Banned.Qbittorrent.Models.Requests.AddTorrentRequest;

namespace TotoroNext.Torrents.Qbittorrent;

public class Client(IModuleSettings<Settings> settings) : ITorrentClient
{
    public async Task AddTorrent(AddTorrentRequest request)
    {
        using var client = await CreateClient();
        await client.Torrent.AddTorrent(new QbAddTorrentRequest
        {
            Urls = [..request.Torrents],
            SavePath = request.SaveDirectory,
            Tags = request.Tags
        });
    }

    public async IAsyncEnumerable<TorrentViewModel> GetTorrents([EnumeratorCancellation] CancellationToken ct)
    {
        using var client = await CreateClient();
        var mainData = await client.Sync.GetMainData();
        if (mainData is null)
        {
            yield break;
        }

        foreach (var torrent in ConvertData(mainData))
        {
            yield return torrent;
        }

        var rid = mainData.Rid;
        while (!ct.IsCancellationRequested)
        {
            var current = await client.Sync.GetMainData(rid);
            foreach (var torrent in ConvertData(mainData))
            {
                yield return torrent;
            }

            rid = current?.Rid ?? 0;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    
    private async Task<QBittorrentClient> CreateClient()
    {
        return await QBittorrentClient.Create(settings.Value.Url, settings.Value.Username, settings.Value.Password);
    }
    
    private static IEnumerable<TorrentViewModel> ConvertData(MainData? mainData)
    {
        var torrents = mainData?.Torrents ?? [];
        foreach (var model in torrents.Select(ConvertModel))
        {
            yield return model;
        }
    }

    private static TorrentViewModel ConvertModel(KeyValuePair<string, TorrentInfo> info)
    {
        return new TorrentViewModel
        {
            Name = info.Value.Name ?? string.Empty,
            Seeders = info.Value.NumSeeds ?? 0,
            Leechers = info.Value.NumLeechs ?? 0,
            Progress = info.Value.Progress ?? 0,
            DownloadSpeed = info.Value.DownloadSpeed ?? 0,
            Hash = info.Key
        };
    }
}