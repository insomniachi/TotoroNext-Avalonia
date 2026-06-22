using System.Xml.Linq;
using FFMpegCore;
using Flurl.Http;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions;

public class FfmpegDownloader : BaseDownloader
{
    protected override IDownloadOperation CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        var input = server.Url;
        var output = Path.Combine(FileHelper.GetPath("Downloads"), filepath);
        var operation = new FfmpegDownloadOperation(input, output)
        {
            Link = input,
            FileName = output
        };
        return operation;
    }
}

public class FfmpegDownloadOperation(Uri input, string output) : BaseDownloadOperation
{
    private Action? _cancel;

    public override async Task StartAsync()
    {
        TotalBytes = await EstimateTotalBytesAsync(input);
        await FFMpegArguments
              .FromUrlInput(input)
              .OutputToFile(output, true, options => options
                                                     .WithVideoCodec("copy")
                                                     .WithAudioCodec("copy"))
              .NotifyOnOutput(ParseProgress)
              .CancellableThrough(out _cancel, 100)
              .ProcessAsynchronously();
    }

    private void ParseProgress(string? line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        if (line.StartsWith("total_size="))
        {
            DownloadedBytes = long.Parse(line.Split('=')[1]);
            if (TotalBytes > 0)
            {
                Progress = (double)DownloadedBytes / TotalBytes * 100.0;
            }
        }
        else if (line.StartsWith("speed="))
        {
            var val = line.Split('=')[1].Replace("x", "");
            if (double.TryParse(val, out var spd))
            {
                Speed = spd;
            }
        }
        else if (line.StartsWith("progress="))
        {
            if (line.Contains("end"))
            {
                IsCompleted = true;
            }
        }
    }


    private static async Task<long> EstimateTotalBytesAsync(Uri mpdUrl)
    {
        var xml = await mpdUrl.GetStringAsync();
        var doc = XDocument.Parse(xml);

        long totalBytes = 0;

        var segments = doc.Descendants()
                          .Where(e => e.Name.LocalName == "SegmentURL")
                          .Select(e => e.Attribute("media")?.Value)
                          .Where(v => !string.IsNullOrEmpty(v));

        foreach (var media in segments)
        {
            var segmentUrl = new Uri(mpdUrl, media).ToString();
            var resp = await segmentUrl.GetAsync(HttpCompletionOption.ResponseHeadersRead);
            if (!resp.Headers.TryGetFirst("Content-Length", out var length))
            {
                continue;
            }

            if (long.TryParse(length, out var size))
            {
                totalBytes += size;
            }

            totalBytes += size;
        }

        return totalBytes;
    }


    protected override void TogglePauseResumeImpl()
    {
        throw new NotSupportedException("Pause and resume is not supported for ffmpeg downloads.");
    }

    protected override void CancelImpl()
    {
        _cancel?.Invoke();
    }
}