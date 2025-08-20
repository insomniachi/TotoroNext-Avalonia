using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Module;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeInfoViewModel(InfoViewNavigationParameters parameters) : ObservableObject, IInitializable
{
    [ObservableProperty] public partial List<KeyValuePair<string, string>> Fields { get; set; } = [];
    
    public void Initialize()
    {
        Fields =
        [
            new KeyValuePair<string, string>("English", parameters.Anime.EngTitle),
            new KeyValuePair<string, string>("Romaji", parameters.Anime.RomajiTitle),
            new KeyValuePair<string, string>("Format", parameters.Anime.MediaFormat.ToString()),
            new KeyValuePair<string, string>("Episodes", parameters.Anime.TotalEpisodes?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Season", $"{parameters.Anime.Season?.SeasonName} {parameters.Anime.Season?.Year}"),
            new KeyValuePair<string, string>("Score", parameters.Anime.MeanScore?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Popularity", parameters.Anime.Popularity.ToString("N0")),
            new KeyValuePair<string, string>("Genres", string.Join(", ", parameters.Anime.Genres))
        ];
    }
}