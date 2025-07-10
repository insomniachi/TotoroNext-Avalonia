using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Anilist;

internal class AnilistTrackingService(GraphQLHttpClient client) : ITrackingService
{
    public string ServiceName { get; } = nameof(ExternalIds.Anilist);

    public async Task<List<AnimeModel>> GetUserList()
    {
        string? userName;
        try
        {
            userName = await FetchUserName();
        }
        catch
        {
            return [];
        }

        if (string.IsNullOrEmpty(userName))
        {
            return [];
        }

        var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
        {
            Query = new QueryQueryBuilder().WithMediaListCollection(MediaListCollectionBuilder(), userName: userName, type: MediaType.Anime).Build(),
        });


        return [.. response.Data.MediaListCollection.Lists.SelectMany(x => x.Entries).Select(x => AniListModelToAnimeModelConverter.ConvertModel(x.Media))];
    }

    public async Task<bool> Remove(long id)
    {
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
            .WithMediaListEntry(new MediaListQueryBuilder().WithId()),
            id: (int)id,
            type: MediaType.Anime).Build();

        var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
        {
            Query = query
        });

        if (response is { Data.Media.MediaListEntry: null } or null)
        {
            return false;
        }

        var trackingId = response.Data.Media.MediaListEntry.Id;

        if (trackingId is null)
        {
            return false;
        }

        query = new MutationQueryBuilder()
            .WithDeleteMediaListEntry(new DeletedQueryBuilder().WithAllFields(), id: trackingId)
            .Build();

        var mutationResponse = await client.SendMutationAsync<Mutation>(new GraphQL.GraphQLRequest
        {
            Query = query
        });

        return mutationResponse.Data?.DeleteMediaListEntry?.Deleted ?? false;
    }

    public async Task<Tracking> Update(long id, Tracking tracking)
    {
        var mediaListEntryBuilder = new MediaListQueryBuilder();

        mediaListEntryBuilder.WithNotes();

        if (tracking.Status is ListItemStatus)
        {
            mediaListEntryBuilder.WithStatus();
        }
        if (tracking.StartDate is DateTime)
        {
            mediaListEntryBuilder.WithStartedAt(new FuzzyDateQueryBuilder().WithAllFields());
        }
        if (tracking.FinishDate is DateTime)
        {
            mediaListEntryBuilder.WithCompletedAt(new FuzzyDateQueryBuilder().WithAllFields());
        }
        if (tracking.Score is int)
        {
            mediaListEntryBuilder.WithScore();
        }
        if (tracking.WatchedEpisodes is int)
        {
            mediaListEntryBuilder.WithProgress();
        }

        var query = new MutationQueryBuilder()
            .WithSaveMediaListEntry(mediaListEntryBuilder,
                status: AniListModelToAnimeModelConverter.ConvertListStatus(tracking.Status),
                startedAt: AniListModelToAnimeModelConverter.ConvertDate(tracking.StartDate),
                completedAt: AniListModelToAnimeModelConverter.ConvertDate(tracking.FinishDate),
                scoreRaw: tracking.Score * 10,
                progress: tracking.WatchedEpisodes,
                mediaId: (int)id,
                notes: "#Totoro")
            .Build();

        var response = await client.SendMutationAsync<Mutation>(new GraphQL.GraphQLRequest
        {
            Query = query
        });

        return AniListModelToAnimeModelConverter.ConvertTracking(response.Data.SaveMediaListEntry) ?? tracking;
    }

    private async Task<string?> FetchUserName()
    {
        var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
        {
            Query = new QueryQueryBuilder().WithViewer(new UserQueryBuilder().WithName()).Build(),
        });

        return response?.Data?.Viewer?.Name;
    }

    private static MediaListCollectionQueryBuilder MediaListCollectionBuilder()
    {
        return new MediaListCollectionQueryBuilder()
                .WithLists(new MediaListGroupQueryBuilder()
                    .WithEntries(new MediaListQueryBuilder()
                        .WithMedia(new MediaQueryBuilder()
                            .WithId()
                            .WithIdMal()
                            .WithFormat()
                            .WithTitle(new MediaTitleQueryBuilder()
                                .WithEnglish()
                                .WithNative()
                                .WithRomaji())
                            .WithCoverImage(new MediaCoverImageQueryBuilder()
                                .WithLarge())
                            .WithEpisodes()
                            .WithStatus()
                            .WithMeanScore()
                            .WithPopularity()
                            .WithDescription()
                            .WithTrailer(new MediaTrailerQueryBuilder()
                                .WithSite()
                                .WithThumbnail()
                                .WithId())
                            .WithGenres()
                            .WithStartDate(new FuzzyDateQueryBuilder().WithAllFields())
                            .WithEndDate(new FuzzyDateQueryBuilder().WithAllFields())
                            .WithSeason()
                            .WithSeasonYear()
                            .WithNextAiringEpisode(new AiringScheduleQueryBuilder()
                                .WithEpisode()
                                .WithTimeUntilAiring())
                            .WithMediaListEntry(new MediaListQueryBuilder()
                                .WithScore()
                                .WithStatus()
                                .WithStartedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                .WithCompletedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                .WithProgress()))));
    }
}
