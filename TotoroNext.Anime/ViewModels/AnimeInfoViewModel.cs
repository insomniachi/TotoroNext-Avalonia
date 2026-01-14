using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
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
        var service = metadataServiceFactory.CreateFor(parameters.Anime);
        if (service is null)
        {
            return;
        }

        Anime = await service.GetAnimeAsync(parameters.Anime.Id);
        Fields = GetFields(Anime, service is ILocalMetadataService).ToList();
    }

    private static IEnumerable<KeyValuePair<string, string>> GetFields(AnimeModel anime, bool normalizeTitles)
    {
        if (normalizeTitles)
        {
            var entries = anime.AlternateTitles.Select(t => new
            {
                Original = t,
                Normalized = Normalize(t)
            }).ToList();
            var groups = entries.GroupBy(e => e.Normalized);
            var distinct = groups.Where(x => !string.IsNullOrEmpty(x.Key))
                                 .Select(g => g.First().Original)
                                 .ToList();
            yield return new KeyValuePair<string, string>("Alternate Titles", string.Join(Environment.NewLine, distinct));
        }
        else
        {
            yield return new KeyValuePair<string, string>("Alternate Titles", string.Join(Environment.NewLine, anime.AlternateTitles));
        }

        yield return new KeyValuePair<string, string>("Format", anime.MediaFormat.ToString());
        if (anime.MediaFormat != AnimeMediaFormat.Movie)
        {
            yield return new KeyValuePair<string, string>("Episodes", anime.TotalEpisodes?.ToString() ?? "??");
        }

        yield return new KeyValuePair<string, string>("Season", $"{anime.Season?.SeasonName} {anime.Season?.Year}");
        yield return new KeyValuePair<string, string>("Score", anime.MeanScore?.ToString() ?? "??");
        yield return new KeyValuePair<string, string>("Popularity", anime.Popularity.ToString("N0"));
        yield return new KeyValuePair<string, string>("Studios", string.Join(",", anime.Studios));
    }

    private static string Normalize(string title)
    {
        if (TextHelpers.IsNotEnglishOrRomaji(title))
        {
            return string.Empty;
        }
        
        title = title.ToLowerInvariant();

        // remove foreign language season indicators
        if (title.Contains("saison") || title.Contains("staffel") || title.Contains("temporada"))
        {
            return string.Empty;
        }

        title = NonAlphaNumeric().Replace(title, "");
        title = MultipleSpaces().Replace(title, " ");
        title = title.Replace("season", "s")
                     .Replace("cour", "s")
                     .Replace("chapter", "s")
                     .Replace("part", "s");
        return title.Trim();
    }

    [GeneratedRegex(@"[^a-z0-9\s]")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}