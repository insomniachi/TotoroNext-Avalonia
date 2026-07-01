using System.Runtime.CompilerServices;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.LocalStorage;

public class AnimeProvider : IAnimeProvider
{
    private static readonly string[] SupportedExtensions = [".mp4", ".mkv", ".avi", ".flv", ".mov", ".webm", ".m4v", ".ts"];

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var rootDirectory = FileHelper.GetPath("Downloads");
        var directories = await Task.Run(() => Directory.GetDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly), ct);

        foreach (var directory in directories.Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new SearchResult(this, directory, Path.GetFileName(directory));
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var files = await Task.Run(() =>
                                       Directory.EnumerateFiles(animeId, "*", SearchOption.TopDirectoryOnly)
                                                .Where(x => SupportedExtensions.Contains(Path.GetExtension(x).ToLower()))
                                                .ToList(), ct);

        foreach (var episode in files
                                .Select(x =>
                                {
                                    var name = Path.GetFileNameWithoutExtension(x);
                                    return float.TryParse(name, out var number)
                                        ? new Episode(this, animeId, x, number)
                                        : null;
                                })
                                .Where(x => x != null)
                                .Select(x => x!)
                                .OrderBy(x => x.Number))
        {
            yield return episode;
        }
    }

    public IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, CancellationToken ct)
    {
        return AsyncEnumerable.Repeat(new VideoServer("Default", new Uri(episodeId)), 1);
    }
}