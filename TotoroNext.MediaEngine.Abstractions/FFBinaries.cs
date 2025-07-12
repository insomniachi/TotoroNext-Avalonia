using System.IO.Compression;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace TotoroNext.MediaEngine.Abstractions;

public static class FfBinaries
{
    public static async Task DownloadLatest()
    {
        var files = Directory.GetFiles(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!);

        if (files.Any(x => x.Contains("ffprob")))
        {
            return;
        }

        using var client = new HttpClient();

        var release = await client.GetFromJsonAsync<FfBinaryRelease>("https://ffbinaries.com/api/v1/version/latest");

        if (release is null)
        {
            return;
        }

        FfBinary? bin = null;
        if (OperatingSystem.IsWindows())
        {
            bin = release.Bin.Windows;
        }
        else if (OperatingSystem.IsLinux())
        {
            bin = release.Bin.Linux;
        }
        else if (OperatingSystem.IsMacOS())
        {
            bin = release.Bin.Mac;
        }

        if (bin is null)
        {
            return;
        }

        var stream = await client.GetStreamAsync(bin.FfProb);
        ZipFile.ExtractToDirectory(stream, ".", true);
    }
}

public class FfBinaryRelease
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("permalink")]
    public string Permalink { get; set; } = "";

    [JsonPropertyName("bin")]
    public FfReleasePlatforms Bin { get; set; } = new();
}

public class FfReleasePlatforms
{
    [JsonPropertyName("windows-64")]
    public FfBinary Windows { get; set; } = new();

    [JsonPropertyName("linux-64")]
    public FfBinary Linux { get; set; } = new();

    [JsonPropertyName("osx-64")]
    public FfBinary Mac { get; set; } = new();
}

public class FfBinary
{
    [JsonPropertyName("ffmpeg")]
    public string FfMpeg { get; set; } = "";

    [JsonPropertyName("ffprobe")]
    public string FfProb { get; set; } = "";
}

