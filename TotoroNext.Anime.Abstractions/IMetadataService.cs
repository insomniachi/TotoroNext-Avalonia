using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IMetadataService
{
    Task<AnimeModel> GetAnimeAsync(long id);
    Task<List<AnimeModel>> SearchAnimeAsync(string term);
    Task<List<AnimeModel>> GetSeasonalAnimeAsync();
    Task<List<AnimeModel>> GetAiringAnimeAsync();
}

[DebuggerDisplay("{Title}")]
public partial class AnimeModel : ObservableObject
{
    public long Id { get; set; }
    public ExternalIds ExternalIds { get; set; } = new();
    public string Image { get; set; } = "";
    public string Title { get; set; } = "";
    public string EngTitle { get; set; } = "";
    public string RomajiTitle { get; set; } = "";
    [ObservableProperty] public partial Tracking? Tracking { get; set; }
    public int? TotalEpisodes { get; set; }
    public AiringStatus AiringStatus { get; set; }
    public float? MeanScore { get; set; }
    public int Popularity { get; set; }
    public DateTime? NextEpisodeAt { get; set; }
    public int AiredEpisodes { get; set; }
    public Season? Season { get; set; }
    public string? ServiceType { get; init; }
    public string Description { get; set; } = "";
    public IEnumerable<AnimeModel> Related { get; set; } = [];
    public IEnumerable<AnimeModel> Recommended { get; set; } = [];
}

public class ExternalIds
{
    public long? MyAnimeList { get; set; }
    public long? Anilist { get; set; }

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
    public int? Score { get; set; }
    public int? WatchedEpisodes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FinishDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Tracking Clone()
    {
        return new()
        {
            StartDate = StartDate,
            Score = Score,
            WatchedEpisodes = WatchedEpisodes,
            Status = Status,
            FinishDate = FinishDate,
            UpdatedAt = UpdatedAt,
        };
    }
}


public enum ListItemStatus
{
    [Description("Watching")]
    Watching,

    [Description("Completed")]
    Completed,

    [Description("On-Hold")]
    OnHold,

    [Description("Plan to Watch")]
    PlanToWatch,

    [Description("Dropped")]
    Dropped,

    [Description("Rewatching")]
    Rewatching,

    [Description("Select status")]
    None
}

public enum AiringStatus
{
    [Description("Finished Airing")]
    FinishedAiring,

    [Description("Currently Airing")]
    CurrentlyAiring,

    [Description("Not Yet Aired")]
    NotYetAired
}
