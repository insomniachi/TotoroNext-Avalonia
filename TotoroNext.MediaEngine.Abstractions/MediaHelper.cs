using System.Diagnostics;
using System.Reflection;

namespace TotoroNext.MediaEngine.Abstractions;

public static class MediaHelper
{
    public static TimeSpan GetDuration(Uri url, IDictionary<string, string>? headers = null)
    {
        var ffprobePath = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
                                   .FirstOrDefault(x => x.Contains("ffprobe"))!;

        var startInfo = new ProcessStartInfo
        {
            FileName = ffprobePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (headers is { Count: > 0 })
        {
            startInfo.ArgumentList.Add("-headers");
            startInfo.ArgumentList.Add(string.Join("\r\n", headers.Select(x => $"{x.Key}: {x.Value}")));
        }

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(url.ToString());
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("format=duration");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("quiet");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("csv=p=0");

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (double.TryParse(output.Trim(), out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.Zero;
    }

    public static IEnumerable<MediaSegment> MakeContiguousSegments(this List<MediaSegment> segments,
                                                                   TimeSpan mediaLength)
    {
        if (segments.Count == 0)
        {
            return segments;
        }
        
        var newSegments = new List<MediaSegment>();
        for (var i = 0; i < segments.Count - 1; i++)
        {
            var current = segments[i];
            var next = segments[i + 1];
            if (current.End != next.Start)
            {
                newSegments.Add(new MediaSegment(MediaSectionType.Content, current.End, next.Start));
            }
        }

        var last = segments.Last();
        if (last.End < mediaLength)
        {
            newSegments.Add(new MediaSegment(MediaSectionType.Content, last.End, mediaLength));
        }

        segments.AddRange(newSegments);

        return segments.OrderBy(x => x.Start);
    }
}