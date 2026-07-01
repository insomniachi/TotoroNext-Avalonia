using Flurl;
using Flurl.Http;
using ManuHub.Ytdlp.NET;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class YtdlpDownloadOperation(VideoServer server, string output) : BaseDownloadOperation
{
    public required string YtdlpPath { get; init; }
    
    public override async Task StartAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(output);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }
            
            if (!string.IsNullOrEmpty(server.Subtitle))
            {
                var subtitleExt = Path.GetExtension(server.Subtitle);
                var subtitlePath = Path.Combine(dir!, Path.ChangeExtension(output, subtitleExt));

                var request = new FlurlRequest(server.Subtitle);
                request = server.Headers.Aggregate(request, (current, kvp) => current.WithHeader(kvp.Key, kvp.Value));

                var stream = await request.GetStreamAsync();
                await using var fileStream = new FileStream(subtitlePath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);
            }

            var downloader = new Ytdlp(YtdlpPath)
                .WithOutputFolder(dir!)
                .WithOutputTemplate(Path.GetFileName(output));

            downloader = server.Headers.Aggregate(downloader, (current, kvp) => current.WithAddHeader(kvp.Key, kvp.Value));

            downloader.ProgressDownload += (_, e) =>
            {
                Progress = e.Percent;
            };

            DownloadStarted = true;
            await downloader.DownloadAsync(server.Url.ToString());
            IsCompleted = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override void TogglePauseResumeImpl()
    {
        throw new NotSupportedException("Pause and resume is not supported for ffmpeg downloads.");
    }

    protected override void CancelImpl()
    {
        throw new NotSupportedException("Cancel is not supported for ffmpeg downloads.");
    }
}