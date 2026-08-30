using System.Text;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IAnimeMusicService
{
    Task<List<AnimeMusic>> FindAll(AnimeModel anime);
}

public class AnimeMusic
{
    public Uri? Video { get; set; }
    public Uri? Audio { get; set; }
    public string? Type { get; init; }
    public string SongName { get; init; } = "";
    public string Artist { get; set; } = "";

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