using Flurl.Http;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class AnimeRelationsParser(IAnimeRelations relations) : IInitializer, IBackgroundInitializer
{
    private readonly string _path = ModuleHelper.GetFilePath(null, "relations.txt");

    public async Task BackgroundInitializeAsync()
    {
        if (!File.Exists(_path))
        {
            await DownloadAndInitialize();
        }
        else
        {
            var fileInfo = new FileInfo(_path);
            if ((DateTime.Now - fileInfo.CreationTime) > TimeSpan.FromDays(30))
            {
                await DownloadAndInitialize();
            }
        }
    }

    public void Initialize()
    {
        Parse();
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

    private void Parse()
    {
        if (!File.Exists(_path))
        {
            return;
        }
        
        foreach (var line in File.ReadLines(_path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("- "))
            {
                trimmed = trimmed["- ".Length ..];
            }

            var arrowIndex = trimmed.IndexOf("->", StringComparison.Ordinal);
            if (arrowIndex == -1)
            {
                continue;
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
                relations.AddRelation(new AnimeRelation
                {
                    SourceIds = ParseIds(srcParts[0]),
                    DestinationIds = ParseIds(dstParts[0]),
                    SourceEpisodesRage = ParseEpisodes(srcParts[1]),
                    DestinationEpisodesRage = ParseEpisodes(dstParts[1])
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private static ExternalIds ParseIds(string rulePart)
    {
        var ids = rulePart.Split('|');
        return new ExternalIds
        {
            MyAnimeList = long.TryParse(ids[0], out var malId) ? malId : null,
            Kitsu = long.TryParse(ids[1], out var kitsuId) ? kitsuId : null,
            Anilist = long.TryParse(ids[2], out var anilistId) ? anilistId : null,
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
    public ExternalIds SourceIds { get; init; } = new();
    public ExternalIds DestinationIds { get; init; } = new();
    public EpisodeRange SourceEpisodesRage { get; init; } = new(0, 0);
    public EpisodeRange DestinationEpisodesRage { get; init; } = new(0, 0);
}

[Serializable]
public class EpisodeRange(int start, int end)
{
    public int Start { get; set; } = start;
    public int End { get; set; } = end;
}