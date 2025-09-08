using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotoroNext.MediaEngine.Abstractions;

public static class MediaHelper
{
    public static async Task<List<MediaSegment>> GetChapters(Uri url, IDictionary<string, string>? headers = null)
    {
        var executable = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
                                  .FirstOrDefault(x => x.Contains("ffprobe"))!;

        if (string.IsNullOrEmpty(executable))
        {
            return [];
        }
        
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(url.ToString());
        psi.ArgumentList.Add("-show_chapters");
        psi.ArgumentList.Add("-print_format");
        psi.ArgumentList.Add("json");
        
        if (headers is { Count: > 0 })
        {
            psi.ArgumentList.Add("-headers");
            psi.ArgumentList.Add(string.Join("\r\n", headers.Select(x => $"{x.Key}: {x.Value}")));
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            return [];
        }
        
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = JsonSerializer.Deserialize<ChapterList>(output);
        if (result is null)
        {
            return [];
        }
        
        var segments = new List<MediaSegment>();
        foreach (var chapter in result.Chapters)
        {
            var start = TimeSpan.FromSeconds(double.Parse(chapter.StartTime));
            var end = TimeSpan.FromSeconds(double.Parse(chapter.EndTime));
            var type = GetType(chapter.Tags.Title);

            if (type is not { } segmentType)
            {
                continue;
            }
            
            segments.Add(new MediaSegment(segmentType, start, end));
        }

        return segments;
    }
    
    public static TimeSpan GetDuration(Uri url, IDictionary<string, string>? headers = null)
    {
        var executable = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
                                   .FirstOrDefault(x => x.Contains("ffprobe"))!;

        if (string.IsNullOrEmpty(executable))
        {
            return TimeSpan.Zero;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
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

        return double.TryParse(output.Trim(), out var seconds) ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
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

    private static MediaSectionType? GetType(string sectionName)
    {
        return sectionName switch
        {
            _ when sectionName.Equals("Intro", StringComparison.InvariantCultureIgnoreCase) => MediaSectionType.Opening,
            _ when sectionName.Equals("Credits", StringComparison.InvariantCultureIgnoreCase) => MediaSectionType.Ending,
            _ when sectionName.Equals("Outro", StringComparison.InvariantCultureIgnoreCase) => MediaSectionType.Ending,
            _ => null
        };
    }
}

[Serializable]
public class ChapterTag
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
}

[Serializable]
public class Chapter
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("time_base")] public string TimeBase { get; set; } = "";

    [JsonPropertyName("start")] public long Start { get; set; }

    [JsonPropertyName("start_time")] public string StartTime { get; set; } = "";

    [JsonPropertyName("end")] public long End { get; set; }

    [JsonPropertyName("end_time")] public string EndTime { get; set; } = "";

    [JsonPropertyName("tags")] public ChapterTag Tags { get; set; } = new();
}

[Serializable]
public class ChapterList
{
    [JsonPropertyName("chapters")] public List<Chapter> Chapters { get; set; } = [];
}