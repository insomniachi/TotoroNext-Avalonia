using MalApi;
using MalApi.Interfaces;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList;

internal class MyAnimeListTrackingService : ITrackingService
{
    private static readonly string[] FieldNames =
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
        AnimeFieldNames.EndDate,
        AnimeFieldNames.StartDate,
        AnimeFieldNames.MediaType
    ];

    private readonly IMalClient _client;
    private readonly Settings _settings;

    public MyAnimeListTrackingService(IMalClient client, IModuleSettings<Settings> settings)
    {
        client.SetClientId(Settings.ClientId);

        if (settings.Value.Auth is { } token)
        {
            client.SetAccessToken(token.AccessToken);
        }

        _client = client;
        _settings = settings.Value;
    }

    public Guid Id { get; } = Module.Id;

    public string Name { get; } = nameof(ExternalIds.MyAnimeList);

    public async Task<List<AnimeModel>> GetUserList()
    {
        var request = _client.Anime()
                             .OfUser()
                             .WithFields(FieldNames);

        if (_settings.IncludeNsfw)
        {
            request.IncludeNsfw();
        }

        var result = await request.Find();

        var response = new List<AnimeModel>();

        response.AddRange(result.Data.Select(MalToModelConverter.ConvertModel));

        while (!string.IsNullOrEmpty(result.Paging?.Next))
        {
            result = await _client.GetNextAnimePage(result);

            response.AddRange(result.Data.Select(MalToModelConverter.ConvertModel));
        }

        return response;
    }

    public async Task<bool> Remove(long id)
    {
        return await _client.Anime().WithId(id).RemoveFromList();
    }

    public async Task<Tracking> Update(long id, Tracking tracking)
    {
        var request = _client.Anime().WithId(id).UpdateStatus().WithTags("Totoro");

        if (tracking.WatchedEpisodes is { } ep)
        {
            request.WithEpisodesWatched(ep);
        }

        if (tracking.Status is { } status)
        {
            if (status == ListItemStatus.Rewatching)
            {
                request.WithIsRewatching(true);
            }
            else
            {
                request.WithStatus((AnimeStatus)(int)status);
            }
        }

        if (tracking.Score is { } score)
        {
            request.WithScore((Score)score);
        }

        if (tracking.StartDate is { } sd)
        {
            request.WithStartDate(sd);
        }

        if (tracking.FinishDate is { } fd)
        {
            request.WithFinishDate(fd);
        }

        try
        {
            var response = await request.Publish();

            var newTracking = new Tracking
            {
                WatchedEpisodes = response.WatchedEpisodes,
                Status = (ListItemStatus)(int)response.Status,
                Score = (int)response.Score,
                UpdatedAt = response.UpdatedAt
            };

            return newTracking;
        }
        catch
        {
            return tracking;
        }
    }
}