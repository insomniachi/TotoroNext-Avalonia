using ManuHub.Ytdlp.NET;

namespace TotoroNext.Anime.Abstractions;

public class YtdlpDownloadOperation(Uri input, IDictionary<string, string> headers, string output) : BaseDownloadOperation
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

            var downloader = new Ytdlp(YtdlpPath)
                .WithOutputFolder(dir!)
                .WithOutputTemplate(Path.GetFileName(output));

            downloader = headers.Aggregate(downloader, (current, kvp) => current.WithAddHeader(kvp.Key, kvp.Value));

            downloader.ProgressDownload += (_, e) =>
            {
                Progress = e.Percent;
            };

            DownloadStarted = true;
            await downloader.DownloadAsync(input.ToString());
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