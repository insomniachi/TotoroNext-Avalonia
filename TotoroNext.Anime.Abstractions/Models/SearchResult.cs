using System.Diagnostics;

namespace TotoroNext.Anime.Abstractions.Models;

[DebuggerDisplay("{Title}")]
public class SearchResult(IAnimeProvider provider, string id, string title, Uri? image = null)
{
    public string Id { get; } = id;
    public string Title { get; } = title;
    public Uri? Image { get; } = image;
    public AnimeId ExternalId { get; init; } = new();

    public IAsyncEnumerable<Episode> GetEpisodes()
    {
        return provider.GetEpisodes(Id);
    }
}