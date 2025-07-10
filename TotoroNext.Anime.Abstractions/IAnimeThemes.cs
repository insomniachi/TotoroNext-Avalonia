using System.Text;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeThemes
{
    Task<List<AnimeTheme>> FindById(long id, string serviceName);
}

public class AnimeTheme
{
    public Uri? Video { get; init; }
    public Uri? Audio { get; init; }
    public AnimeThemeType Type { get; init; }
    public string Slug { get; init; } = "";
    public string SongName { get; init; } = "";
    public string Artist { get; init; } = "";

    public string GetDisplayName()
    {
        var sb = new StringBuilder();
        sb.Append($"({Slug}) - {SongName}");
        if (!string.IsNullOrEmpty(Artist))
        {
            sb.Append($" by {Artist}");
        }
        return sb.ToString();
    }
}

public enum AnimeThemeType
{
    OP,
    ED
}
