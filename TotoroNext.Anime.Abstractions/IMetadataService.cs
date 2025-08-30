using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IMetadataService
{
    Guid Id { get; }
    public string Name { get; }
    Task<AnimeModel> GetAnimeAsync(long id);
    Task<List<AnimeModel>> SearchAnimeAsync(string term);
    Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request);
    Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime);
    Task<List<string>> GetGenresAsync();
}

public static class MetadataServiceExtensions
{
    public static async ValueTask<AnimeModel?> FindAnimeAsync(this IMetadataService service, AnimeModel anime)
    {
        if (anime.ServiceId == service.Id)
        {
            return anime;
        }

        var response = await service.SearchAnimeAsync(anime.Title);

        return response switch
        {
            { Count: 0 } => null,
            { Count: 1 } => response[0],
            _ => response.FirstOrDefault(x => x.Season == anime.Season)
        };
    }
}

public class AdvancedSearchRequest
{
    public string? Title { get; init; }
    public AnimeSeason? SeasonName { get; init; }
    public AnimeSource? Source { get; init; }
    public List<string>? IncludedGenres { get; init; }
    public List<string>? ExcludedGenres { get; init; }
    public float? MinimumScore { get; init; }
    public float? MaximumScore { get; init; }
    public int? MaxYear { get; init; }
    public int? MinYear { get; init; }
}

public class ScheduledAnime(AnimeModel anime)
{
    public DateTime Start { get; init; }
    public AnimeModel Anime { get; } = anime;
}

[DebuggerDisplay("{Title}")]
public partial class AnimeModel : ObservableObject
{
    public long Id { get; init; }
    public ExternalIds ExternalIds { get; init; } = new();
    public string Image { get; set; } = "";
    public string Title { get; init; } = "";
    public string EngTitle { get; init; } = "";
    public string RomajiTitle { get; init; } = "";
    [ObservableProperty] public partial Tracking? Tracking { get; set; }
    public int? TotalEpisodes { get; set; }
    public AiringStatus AiringStatus { get; set; }
    public float? MeanScore { get; set; }
    public int Popularity { get; set; }
    public DateTime? NextEpisodeAt { get; set; }
    public int AiredEpisodes { get; set; }
    public Season? Season { get; set; }
    public Guid? ServiceId { get; init; }
    public string? ServiceName { get; init; }
    public string Description { get; set; } = "";
    public IEnumerable<AnimeModel> Related { get; set; } = [];
    public IEnumerable<AnimeModel> Recommended { get; set; } = [];
    public List<EpisodeInfo> Episodes { get; set; } = [];
    public string Url { get; init; } = "";
    public AnimeMediaFormat MediaFormat { get; init; } = AnimeMediaFormat.Unknown;
    public IReadOnlyCollection<string> Genres { get; init; } = [];
    public IReadOnlyCollection<string> Studios { get; init; } = [];
}

public class ExternalIds
{
    public long? MyAnimeList { get; init; }
    public long? Anilist { get; set; }
    public long? Kitsu { get; init; }

    public long? GetId(string serviceType)
    {
        if (GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(serviceType, StringComparison.OrdinalIgnoreCase)) is not { } property)
        {
            return null;
        }

        return (long?)property.GetValue(this);
    }
}

public class Tracking
{
    public ListItemStatus? Status { get; set; }
    public int? Score { get; init; }
    public int? WatchedEpisodes { get; set; }
    public DateTime? StartDate { get; init; }
    public DateTime? FinishDate { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public Tracking Clone()
    {
        return new Tracking
        {
            StartDate = StartDate,
            Score = Score,
            WatchedEpisodes = WatchedEpisodes,
            Status = Status,
            FinishDate = FinishDate,
            UpdatedAt = UpdatedAt
        };
    }
}

public enum ListItemStatus
{
    [Description("Watching")] Watching,

    [Description("Completed")] Completed,

    [Description("On Hold")] OnHold,

    [Description("Planning")] PlanToWatch,

    [Description("Dropped")] Dropped,

    [Description("Rewatching")] Rewatching,

    [Description("Select status")] None
}

public enum AiringStatus
{
    [Description("Finished Airing")] FinishedAiring,

    [Description("Currently Airing")] CurrentlyAiring,

    [Description("Not Yet Aired")] NotYetAired
}

public enum AnimeMediaFormat
{
    Unknown,
    Tv,
    Ova,
    Movie,
    Special,
    Ona,
    Music
}

public enum AnimeSource
{
    Original,
    Manga,
    LightNovel,
    VisualNovel,
    VideoGame,
    Other,
    Novel,
    Doujinshi,
    Anime,
    WebNovel,
    LiveAction,
    Game,
    Comic,
    MultimediaProject,
    PictureBook
}