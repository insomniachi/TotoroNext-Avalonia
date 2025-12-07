using AnitomySharp;
using FuzzySharp;

namespace TotoroNext.Torrents.Abstractions;

public class TorrentModel
{
    public required Uri Torrent { get; set; }
    public required string Title { get; init; }
    public int Seeders { get; set; }
    public int Leechers { get; set; }
    public int Downloads { get; set; }
    public string? InfoHash { get; set; }
    public string Size { get; set; } = "";
    public string? ReleaseGroup { get; set; }
    public int? Season { get; set; }
    public int? Episode { get; set; }

    public static TorrentModel? Create(Uri torrent, string title)
    {
        var parsedResults = AnitomySharp.AnitomySharp.Parse(title).ToList();
        var animeTitle = parsedResults.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementAnimeTitle)?.Value;
        var releaseGroup = parsedResults.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementReleaseGroup)?.Value;
        var season = parsedResults.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementAnimeSeason)?.Value;
        var episode = parsedResults.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementEpisodeNumber)?.Value;
        var releaseInfo = parsedResults.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementReleaseInformation)?.Value;

        if (releaseInfo == "BATCH")
        {
            return null;
        }

        if (releaseGroup is not null &&
            animeTitle is not null &&
            Fuzz.PartialRatio(animeTitle, releaseGroup) > 50)
        {
            releaseGroup = "";
        }

        return new TorrentModel
        {
            Torrent = torrent,
            Title = title,
            Season = string.IsNullOrEmpty(season) ? null : int.Parse(season),
            Episode = string.IsNullOrEmpty(episode) ? null : int.Parse(episode),
            ReleaseGroup = releaseGroup
        };
    }
}