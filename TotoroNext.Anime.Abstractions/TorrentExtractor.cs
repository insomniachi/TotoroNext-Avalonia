﻿using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class TorrentExtractor(IFactory<IDebrid, Guid> debridFactory) : ITorrentExtractor
{
    public async IAsyncEnumerable<VideoSource> Extract(Uri url)
    {
        var debrid = debridFactory.CreateDefault();
        var directLink = await debrid.TryGetDirectDownloadLink(url);
        yield return new VideoSource
        {
            Url = directLink ?? url
        };
    }

    // public async IAsyncEnumerable<VideoSource> ExtractWithMonoTorrent(Uri url)
    // {
    //     var engine = new ClientEngine(new EngineSettings());
    //     var stream = await url.GetStreamAsync();
    //     using var ms = new MemoryStream();
    //     await stream.CopyToAsync(ms);
    //     ms.Position = 0;
    //     var torrent = await Torrent.LoadAsync(ms);
    //     var manager = await engine.AddStreamingAsync(torrent, "Torrents");
    //     var file = manager.Files.MaxBy(x => x.Length);
    //     await manager.StartAsync();
    //     using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    //     while (!cts.IsCancellationRequested)
    //     {
    //         file ??= manager.Files.MaxBy(x => x.Length);
    //
    //         if (file is null)
    //         {
    //             await Task.Delay(100, cts.Token);
    //             continue;
    //         }
    //
    //         if (!Enumerable.Range(file.StartPieceIndex, 1).All(i => manager.Bitfield[i]))
    //         {
    //             continue;
    //         }
    //
    //         yield return new VideoSource { Url = new Uri(file.FullPath) };
    //         yield break;
    //     }
    // }
}