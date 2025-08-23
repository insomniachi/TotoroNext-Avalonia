using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeInfoViewModel(InfoViewNavigationParameters parameters) : ObservableObject, IInitializable
{
    [ObservableProperty] public partial List<KeyValuePair<string, string>> Fields { get; set; } = [];
    
    [ObservableProperty] public partial AnimeModel? Anime { get; set; }
    
    public void Initialize()
    {
        Anime = parameters.Anime;
        
        Fields =
        [
            new KeyValuePair<string, string>("English", Anime.EngTitle),
            new KeyValuePair<string, string>("Romaji",Anime.RomajiTitle),
            new KeyValuePair<string, string>("Format", Anime.MediaFormat.ToString()),
            new KeyValuePair<string, string>("Episodes", Anime.TotalEpisodes?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Season", $"{Anime.Season?.SeasonName} {Anime.Season?.Year}"),
            new KeyValuePair<string, string>("Score", Anime.MeanScore?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Popularity", Anime.Popularity.ToString("N0")),
        ];
    }
}