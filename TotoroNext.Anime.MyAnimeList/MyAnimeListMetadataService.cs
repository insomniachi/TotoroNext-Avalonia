using Flurl;
using Flurl.Http;
using JikanDotNet;
using MalApi;
using MalApi.Interfaces;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using AnimeSeason = MalApi.AnimeSeason;
using Genre = JikanDotNet.Genre;
using Season = TotoroNext.Anime.Abstractions.Models.Season;

namespace TotoroNext.Anime.MyAnimeList;

internal class MyAnimeListMetadataService : IMetadataService
{
    private const string RecursiveAnimeProperties = $"my_list_status,status,{AnimeFieldNames.TotalEpisodes},{AnimeFieldNames.Mean}";
    private List<Genre> _genres = [];

    private readonly IMalClient _client;
    private readonly IJikan _jikanClient = new Jikan();

    private readonly string[] _commonFields =
    [
        AnimeFieldNames.Synopsis,
        AnimeFieldNames.TotalEpisodes,
        AnimeFieldNames.Broadcast,
        AnimeFieldNames.UserStatus,
        AnimeFieldNames.NumberOfUsers,
        AnimeFieldNames.Rank,
        AnimeFieldNames.Mean,
        AnimeFieldNames.AlternativeTitles,
        AnimeFieldNames.Popularity,
        AnimeFieldNames.StartSeason,
        AnimeFieldNames.Genres,
        AnimeFieldNames.Status,
        AnimeFieldNames.Videos,
        AnimeFieldNames.StartDate,
        AnimeFieldNames.MediaType
    ];

    private readonly Settings _settings;

    public MyAnimeListMetadataService(IMalClient client, IModuleSettings<Settings> settings)
    {
        client.SetClientId(Settings.ClientId);

        if (settings.Value.Auth is { } token)
        {
            client.SetAccessToken(token.AccessToken);
        }

        _client = client;
        _settings = settings.Value;
    }

    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        return await anime.GetEpisodes();
        // var jikanResponse = await _jikanClient.GetAnimeEpisodesAsync(anime.Id);
        // var response = new List<EpisodeInfo>();
        // do
        // {
        //     foreach (var pair in jikanResponse.Data.Index())
        //     {
        //         var (index, jikanEp) = pair;
        //         response.Add(new EpisodeInfo()
        //         {
        //             EpisodeNumber = (int)jikanEp.MalId,
        //             Titles = new Titles()
        //             {
        //                 English = jikanEp.Title,
        //                 Romaji = jikanEp.TitleJapanese,
        //                 Japanese = jikanEp.TitleJapanese
        //             },
        //             Overview = jikanEp.Synopsis,
        //             Runtime = jikanEp.Duration ?? 0,
        //             AirDateUtc = jikanEp.Aired
        //         });
        //     }
        // } while (jikanResponse.Pagination.HasNextPage);
        //
        // return response;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        var uri = new Url("https://api.jikan.moe/v4/anime");
        if (!string.IsNullOrEmpty(request.Title))
        {
            uri.AppendQueryParam("q", request.Title);
        }

        if (request.MaximumScore is { } maxScore)
        {
            uri.AppendQueryParam("max_score", maxScore);
        }

        if (request.MinimumScore is { } minScore)
        {
            uri.AppendQueryParam("min_score", minScore);
        }

        if (request.MinYear is { } minYear)
        {
            uri.AppendQueryParam("start_date", $"{minYear}-01-01");
        }

        if (request.MaxYear is { } maxYear)
        {
            uri.AppendQueryParam("end_date", $"{maxYear}-12-31");
        }

        if (request.IncludedGenres is { Count: > 0 } includedGenres)
        {
            var includedGenreIds = includedGenres.Select(x => _genres.FirstOrDefault(g => g.Name == x)?.MalId).Where(x => x is not null);
            uri.AppendQueryParam("genres",  string.Join(",", includedGenreIds));
        }

        if (request.ExcludedGenres is { Count: > 0 } excludedGenres)
        {
            var excludedGenreIds = excludedGenres.Select(x => _genres.FirstOrDefault(g => g.Name == x)?.MalId).Where(x => x is not null);
            uri.AppendQueryParam("genres_exclude",  string.Join(",", excludedGenreIds));
        }

        var response = await uri.GetJsonAsync<PaginatedJikanResponse<ICollection<JikanDotNet.Anime>>>();
        return [..response.Data.Where(x => x.Year is not null).Select(MalToModelConverter.ConvertJikanModel)];
    }

    public Guid Id { get; } = Module.Id;

    public string Name { get; } = nameof(ExternalIds.MyAnimeList);

    public async Task<AnimeModel> GetAnimeAsync(long id)
    {
        var malModel = await _client.Anime().WithId(id)
                                    .WithFields(_commonFields)
                                    .WithField(x => x.Genres)
                                    .WithFields($"related_anime{{{RecursiveAnimeProperties}}}")
                                    .WithFields($"recommendations{{{RecursiveAnimeProperties}}}")
                                    .Find();

        return MalToModelConverter.ConvertModel(malModel);
    }
    
    public async Task<List<string>> GetGenresAsync()
    {
        var response = await _jikanClient.GetAnimeGenresAsync();
        _genres = response.Data.ToList();
        return [.._genres.Select(x => x.Name).Order()];
    }
    
    public async Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        var request = _client
                      .Anime()
                      .WithName(term)
                      .WithFields(_commonFields)
                      .WithLimit(5);

        if (_settings.IncludeNsfw)
        {
            request.IncludeNsfw();
        }

        var result = await request.Find();

        return [.. result.Data.Select(MalToModelConverter.ConvertModel)];
    }

    public async Task<List<AnimeModel>> GetAiringAnimeAsync()
    {
        var request = _client
                      .Anime()
                      .Top(AnimeRankingType.Airing)
                      .WithLimit(15)
                      .WithFields(_commonFields);

        if (_settings.IncludeNsfw)
        {
            request.IncludeNsfw();
        }

        var result = await request.Find();

        return [.. result.Data.Select(x => MalToModelConverter.ConvertModel(x.Anime))];
    }

    public async Task<List<AnimeModel>> GetAnimeAsync(Season season)
    {
        var request = _client.Anime()
                             .OfSeason((AnimeSeason)(int)season.SeasonName, season.Year)
                             .WithFields(_commonFields);

        if (_settings.IncludeNsfw)
        {
            request.IncludeNsfw();
        }

        var pagedAnime = await request.Find();

        return [.. pagedAnime.Data.Select(MalToModelConverter.ConvertModel)];
    }
}