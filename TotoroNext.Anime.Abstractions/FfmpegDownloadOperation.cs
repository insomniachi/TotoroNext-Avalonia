using System.Diagnostics;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class FfmpegDownloader : BaseDownloader
{
    protected override IDownloadOperation CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        var operation = new FfmpegDownloadOperation(server.Url, server.Headers, filepath)
        {
            Link = server.Url,
            FileName = filepath
        };
        return operation;
    }
}

public class FfmpegDownloadOperation(Uri input, IDictionary<string, string> headers, string output) : BaseDownloadOperation
{
    public override async Task StartAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(output);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }

            var headerString = string.Join(@"\r\n", headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            var args = $"-headers \"{headerString}\" -i \"{input}\" -c copy \"{output}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (_, e) => { ParseProgress(e.Data); };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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


    // private async Task<Uri> GetFirstStream()
    // {
    //     var playlist = await input.WithHeaders(headers).GetStringAsync();
    //     var lines = playlist.Split('\n')
    //                         .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
    //                         .ToList();
    //
    //     if (lines.Count == 0)
    //     {
    //         throw new InvalidOperationException("No streams found in master playlist.");
    //     }
    //
    //     var firstStream = lines[0].Trim();
    //     return new Uri(input, firstStream);
    // }


    protected override void TogglePauseResumeImpl()
    {
        throw new NotSupportedException("Pause and resume is not supported for ffmpeg downloads.");
    }

    protected override void CancelImpl()
    {
        throw new NotSupportedException("Cancel is not supported for ffmpeg downloads.");
    }
}