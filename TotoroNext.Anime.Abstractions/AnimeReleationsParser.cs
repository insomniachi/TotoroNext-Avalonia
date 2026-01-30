using Flurl.Http;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class AnimeRelationsParser(IAnimeRelations relations) : IInitializer, IBackgroundInitializer
{
    private readonly string _path = FileHelper.GetPath("relations.txt");
    private readonly string _localPath = FileHelper.GetPath("relations-local.txt");

    public async Task BackgroundInitializeAsync()
    {
        if (!File.Exists(_path))
        {
            await DownloadAndInitialize();
        }
        else
        {
            var fileInfo = new FileInfo(_path);
            if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(30))
            {
                await DownloadAndInitialize();
            }
        }
    }

    public void Initialize()
    {
        Parse(_path);
        Parse(_localPath);
    }

    public static AnimeRelation? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
        {
            return null;
        }

        if (trimmed.StartsWith("- "))
        {
            trimmed = trimmed["- ".Length ..];
        }

        var arrowIndex = trimmed.IndexOf("->", StringComparison.Ordinal);
        if (arrowIndex == -1)
        {
            return null;
        }

        var src = trimmed[..arrowIndex].Trim();
        var dst = trimmed[(arrowIndex + 2)..].Trim();

        var isSelfRedirect = dst.EndsWith('!');
        if (isSelfRedirect)
        {
            dst = dst[..^1].Trim();
        }

        var srcParts = src.Split(':');
        var dstParts = dst.Split(':');

        try
        {
            return new AnimeRelation
            {
                SourceIds = ParseIds(srcParts[0]),
                DestinationIds = ParseIds(dstParts[0]),
                SourceEpisodesRage = ParseEpisodes(srcParts[1]),
                DestinationEpisodesRage = ParseEpisodes(dstParts[1])
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task DownloadAndInitialize()
    {
        var text =
            await "https://raw.githubusercontent.com/erengy/anime-relations/refs/heads/master/anime-relations.txt"
                .GetStringAsync();

        var directory = Path.GetDirectoryName(_path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_path, text);
        Initialize();
    }

    private void Parse(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            if(ParseLine(line) is not { } relation)
            {
                continue;
            }
            
            relations.AddRelation(relation);
        }
    }

    private static AnimeId ParseIds(string rulePart)
    {
        var ids = rulePart.Split('|');
        return new AnimeId
        {
            MyAnimeList = long.TryParse(ids[0], out var malId) ? malId : 0,
            Kitsu = long.TryParse(ids[1], out var kitsuId) ? kitsuId : 0,
            Anilist = long.TryParse(ids[2], out var anilistId) ? anilistId : 0
        };
    }

    private static EpisodeRange ParseEpisodes(string rulePart)
    {
        var range = rulePart.Split('-');
        if (range.Length == 1)
        {
            return new EpisodeRange(int.Parse(range[0]), int.Parse(range[0]));
        }

        var end = range[1];
        var endInt = end switch
        {
            "?" => int.MaxValue,
            _ => int.Parse(end)
        };

        return new EpisodeRange(int.Parse(range[0]), endInt);
    }
}

[Serializable]
public class AnimeRelation
{
    public AnimeId SourceIds { get; init; } = new();
    public AnimeId DestinationIds { get; init; } = new();
    public EpisodeRange SourceEpisodesRage { get; init; } = new(0, 0);
    public EpisodeRange DestinationEpisodesRage { get; init; } = new(0, 0);
    
    public static bool AreEqual(AnimeRelation first, AnimeRelation second)
    {
        return AnimeId.EqualsForAnimeRelations(first.DestinationIds, second.DestinationIds) &&
               AnimeId.EqualsForAnimeRelations(first.SourceIds, second.SourceIds) &&
               first.DestinationEpisodesRage == second.DestinationEpisodesRage &&
               first.SourceEpisodesRage == second.SourceEpisodesRage;
    }

    public override string ToString()
    {
        return $"- {ConvertIds(SourceIds)}:{SourceEpisodesRage.Start}-{SourceEpisodesRage.End} -> {ConvertIds(DestinationIds)}:{DestinationEpisodesRage.Start}-{DestinationEpisodesRage.End}!";
    }
    
    private static string ConvertIds(AnimeId id)
    {
        var malId = id.MyAnimeList > 0 ? id.MyAnimeList.ToString() : "?";
        var kitsuId = id.Kitsu > 0 ? id.Kitsu.ToString() : "?";
        var anilistId = id.Anilist > 0 ? id.Anilist.ToString() : "?";
        
        return $"{malId}|{kitsuId}|{anilistId}";
    }
}

[Serializable]
public record EpisodeRange(int Start, int End);