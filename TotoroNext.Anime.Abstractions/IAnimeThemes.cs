using System.Text;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeThemes
{
    Task<List<AnimeTheme>> FindAll(AnimeModel anime);
}

public class AnimeTheme
{
    public Uri? Video { get; init; }
    public Uri? Audio { get; init; }
    public string? Type { get; init; }
    public string SongName { get; init; } = "";
    public string Artist { get; init; } = "";

    public string DisplayName
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($"({Type}) - {SongName}");
            if (!string.IsNullOrEmpty(Artist))
            {
                sb.Append($" by {Artist}");
            }

            return sb.ToString();
        }
    }
}