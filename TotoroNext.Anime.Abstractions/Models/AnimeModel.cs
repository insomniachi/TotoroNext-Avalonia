using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Anime.Abstractions.Models;

[DebuggerDisplay("{Title}")]
public partial class AnimeModel : ObservableObject
{
    public long Id { get; init; }
    public AnimeId ExternalIds { get; init; } = new();
    public string Image { get; set; } = "";
    public string BannerImage { get; set; } = "";
    public string Title { get; init; } = "";
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
    public Guid? ServiceId { get; init; }
    public string? ServiceName { get; init; }
    public string Description { get; set; } = "";
    public IEnumerable<AnimeModel> Related { get; set; } = [];
    public IEnumerable<AnimeModel> Recommended { get; set; } = [];
    public List<EpisodeInfo> Episodes { get; init; } = [];
    public string Url { get; init; } = "";
    public AnimeMediaFormat MediaFormat { get; init; } = AnimeMediaFormat.Unknown;
    public IReadOnlyCollection<string> Genres { get; init; } = [];
    public IReadOnlyCollection<string> Studios { get; init; } = [];
    public IReadOnlyCollection<TrailerVideo> Trailers { get; set; } = [];
}