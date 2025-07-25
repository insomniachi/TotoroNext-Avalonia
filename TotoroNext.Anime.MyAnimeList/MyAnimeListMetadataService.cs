using MalApi;
using MalApi.Interfaces;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using AnimeSeason = MalApi.AnimeSeason;
using Season = TotoroNext.Anime.Abstractions.Models.Season;

namespace TotoroNext.Anime.MyAnimeList;

internal class MyAnimeListMetadataService : IMetadataService
{
    private const string RecursiveAnimeProperties = $"my_list_status,status,{AnimeFieldNames.TotalEpisodes},{AnimeFieldNames.Mean}";

    private readonly IMalClient _client;
    // private readonly IJikan _jikanClient = new Jikan();

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

    public Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        return Task.FromResult<List<AnimeModel>>([]);
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