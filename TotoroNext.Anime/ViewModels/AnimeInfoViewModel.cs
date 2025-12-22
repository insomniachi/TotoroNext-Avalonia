using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeInfoViewModel(
    InfoViewNavigationParameters parameters,
    IFactory<IMetadataService, Guid> metadataServiceFactory) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial List<KeyValuePair<string, string>> Fields { get; set; } = [];

    [ObservableProperty] public partial AnimeModel? Anime { get; set; }

    public async Task InitializeAsync()
    {
        var service = metadataServiceFactory.CreateDefault();
        if (service is null)
        {
            return;
        }

        Anime = await service.GetAnimeAsync(parameters.Anime.Id);

        Fields =
        [
            new KeyValuePair<string, string>("Titles", string.Join(Environment.NewLine, Anime.AlternateTitles)),
            new KeyValuePair<string, string>("Format", Anime.MediaFormat.ToString()),
            new KeyValuePair<string, string>("Episodes", Anime.TotalEpisodes?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Season", $"{Anime.Season?.SeasonName} {Anime.Season?.Year}"),
            new KeyValuePair<string, string>("Score", Anime.MeanScore?.ToString() ?? "??"),
            new KeyValuePair<string, string>("Popularity", Anime.Popularity.ToString("N0")),
            new KeyValuePair<string, string>("Studios", string.Join(",", Anime.Studios))
        ];
    }
}