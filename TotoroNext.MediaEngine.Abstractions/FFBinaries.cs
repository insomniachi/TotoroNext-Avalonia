using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Serialization;
using Flurl.Http;

namespace TotoroNext.MediaEngine.Abstractions;

public static class FfBinaries
{
    public static async Task EnsureExists()
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!);
        var release = await "https://ffbinaries.com/api/v1/version/latest".GetJsonAsync<FfBinaryRelease>();

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

        if (!files.Any(x => x.Contains("ffprob")))
        {
            await Download(bin.FfProb);
        }
        
        if (!files.Any(x => x.Contains("ffmpeg")))
        {
            await Download(bin.FfMpeg);
        }
    }

    private static async Task Download(string binary)
    {
        var stream = await binary.GetStreamAsync();
        await ZipFile.ExtractToDirectoryAsync(stream, ".", true);
    }
}

[Serializable]
public class FfBinaryRelease
{
    [JsonPropertyName("version")] public string Version { get; set; } = "";

    [JsonPropertyName("permalink")] public string Permalink { get; set; } = "";

    [JsonPropertyName("bin")] public FfReleasePlatforms Bin { get; set; } = new();
}

public class FfReleasePlatforms
{
    [JsonPropertyName("windows-64")] public FfBinary Windows { get; set; } = new();

    [JsonPropertyName("linux-64")] public FfBinary Linux { get; set; } = new();

    [JsonPropertyName("osx-64")] public FfBinary Mac { get; set; } = new();
}

public class FfBinary
{
    [JsonPropertyName("ffmpeg")] public string FfMpeg { get; set; } = "";

    [JsonPropertyName("ffprobe")] public string FfProb { get; set; } = "";
}