using MalApi;
using MalApi.Interfaces;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module.Abstractions;
using Season = TotoroNext.Anime.Abstractions.Models.Season;

namespace TotoroNext.Anime.MyAnimeList;

internal class MyAnimeListMetadataService : IMetadataService
{
    private readonly IMalClient _client;

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

    private const string RecursiveAnimeProperties = $"my_list_status,status,{AnimeFieldNames.TotalEpisodes},{AnimeFieldNames.Mean}";
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

    public async Task<List<AnimeModel>> GetSeasonalAnimeAsync()
    {
        var current = AnimeHelpers.CurrentSeason();
        var prev = AnimeHelpers.PrevSeason();
        var next = AnimeHelpers.NextSeason();

        var response = new List<AnimeModel>();

        foreach (var season in new[] { current, prev, next })
        {
            var pagedAnime = await BaseRequest(season).Find();

            response.AddRange(pagedAnime.Data.Select(MalToModelConverter.ConvertModel));
        }

        return response;

        IGetSeasonalAnimeListRequest BaseRequest(Season season)
        {
            var request = _client.Anime()
                                 .OfSeason((AnimeSeason)(int)season.SeasonName, season.Year)
                                 .WithFields(_commonFields);

            if (_settings.IncludeNsfw)
            {
                request.IncludeNsfw();
            }

            return request;
        }
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
}