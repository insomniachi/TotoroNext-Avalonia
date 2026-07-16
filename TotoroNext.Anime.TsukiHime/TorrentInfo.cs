using System.Text;
using AnitomySharp;

namespace TotoroNext.Anime.TsukiHime;

internal class TorrentInfo
{
    public string? Resolution { get; init; }
    public required string Group { get; init; }
    public string? Video { get; init; }
    public string? Audio { get; init; }
    public string? Source { get; init; }

    public static TorrentInfo Parse(TorrentDescriptor descriptor)
    {
        var parts = AnitomySharp.AnitomySharp.Parse(descriptor.Name).ToList();
        var resolution = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoResolution)?.Value;
        var encodingVideo = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoTerm)?.Value;
        var encodingAudio = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementAudioTerm)?.Value;
        var source = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementSource)?.Value;
        return new TorrentInfo()
        {
            Group = descriptor.Group.Name,
            Resolution = resolution,
            Video = encodingVideo,
            Audio = encodingAudio,
            Source = source
        };
    }
        
    public string GetDisplayName()
    {
        var sb = new StringBuilder();
        sb.Append($"[{Group}]");
        if (!string.IsNullOrEmpty(Resolution))
        {
            sb.Append($" [{Resolution}]");
        }

        if (!string.IsNullOrEmpty(Source))
        {
            sb.Append($" [{Source}]");
        }

        if (!string.IsNullOrEmpty(Video))
        {
            sb.Append($" [{Video}]");
        }

        if (!string.IsNullOrEmpty(Audio))
        {
            sb.Append($" [{Audio}]");
        }

        return sb.ToString();
    }
}