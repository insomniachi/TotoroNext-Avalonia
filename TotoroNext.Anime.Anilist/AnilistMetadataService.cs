using GraphQL;
using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist;

public interface IAnilistMetadataService : IMetadataService
{
    Task<List<ScheduledAnime>> GetAiringSchedule(int start, int end);
}

internal class AnilistMetadataService(
    GraphQLHttpClient client,
    IModuleSettings<Settings> settings) : IAnilistMetadataService
{
    public Guid Id => Module.Id;

    public string Name => nameof(AnimeId.Anilist);

    public async Task<AnimeModel> GetAnimeAsync(long id)
    {
        var query = new QueryQueryBuilder().WithMedia(MediaQueryBuilderFull(), (int)id,
                                                      type: MediaType.Anime).Build();

        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        return AniListModelToAnimeModelConverter.ConvertModel(response.Data.Media);
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(MediaQueryBuilder(), search: term, type: MediaType.Anime), 1,
                                                         (int)settings.Value.SearchLimit).Build()
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(MediaQueryBuilder(),
                                                                        search: request.Title,
                                                                        season: AniListModelToAnimeModelConverter.ConvertSeason(request.SeasonName),
                                                                        startDateGreater: request.MinYear is { } minYear and > 1950
                                                                            ? int.Parse($"{minYear}0000")
                                                                            : null,
                                                                        endDateLesser: request.MaxYear is { } maxYear and > 1950
                                                                            ? int.Parse($"{maxYear + 1}0000")
                                                                            : null,
                                                                        source: AniListModelToAnimeModelConverter.ConvertSource(request.Source),
                                                                        genreIn: request.IncludedGenres,
                                                                        genreNotIn: request.ExcludedGenres,
                                                                        averageScoreGreater: (int?)(request.MinimumScore * 100),
                                                                        averageScoreLesser: (int?)(request.MaximumScore * 100),
                                                                        sort: new List<MediaSort?> { MediaSort.PopularityDesc, MediaSort.ScoreDesc },
                                                                        type: MediaType.Anime), 1,
                                                         50).Build()
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<string>> GetGenresAsync()
    {
        var query = new QueryQueryBuilder().WithGenreCollection().Build();

        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        return response.Data.GenreCollection.ToList();
    }

    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        return await anime.GetEpisodes();
    }

    public async Task<List<CharacterModel>> GetCharactersAsync(long animeId)
    {
        return await AnilistHelper.GetCharactersAsync(client, animeId);
    }

    public async Task<List<AnimeModel>> GetPopularAnimeAsync()
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(MediaQueryBuilder(),
                                                                        sort: new List<MediaSort?>
                                                                            { MediaSort.TrendingDesc, MediaSort.PopularityDesc },
                                                                        status: MediaStatus.Releasing,
                                                                        type: MediaType.Anime), 1,
                                                         (int)settings.Value.SearchLimit).Build()
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<AnimeModel>> GetAiringToday()
    {
        try
        {
            var start = (int)((DateTimeOffset)DateTime.UtcNow.Date).ToUnixTimeSeconds();
            var end = (int)((DateTimeOffset)DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds();
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithAiringSchedules(new AiringScheduleQueryBuilder()
                                                                                      .WithMedia(MediaQueryBuilder()),
                                                                                  airingAtGreater: start,
                                                                                  airingAtLesser: end,
                                                                                  sort: new List<AiringSort?> { AiringSort.Time }), 1, 20)
                                               .Build()
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.AiringSchedules
                               .Select(x => x.Media)
                               .Where(FilterNsfw)
                               .Select(AniListModelToAnimeModelConverter.ConvertModel)];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<AnimeModel>> GetUpcomingAnimeAsync()
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(MediaQueryBuilder(),
                                                                        sort: new List<MediaSort?> { MediaSort.PopularityDesc },
                                                                        status: MediaStatus.NotYetReleased,
                                                                        type: MediaType.Anime), 1,
                                                         (int)settings.Value.SearchLimit).Build()
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<ScheduledAnime>> GetAiringSchedule(int start, int end)
    {
        var page = 1;
        var result = new List<ScheduledAnime>();
        try
        {
            bool hasNextPage;
            do
            {
                var response = await client.SendQueryAsync<Query>(new GraphQLRequest
                {
                    Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithPageInfo(new PageInfoQueryBuilder()
                                                                               .WithHasNextPage())
                                                             .WithAiringSchedules(new AiringScheduleQueryBuilder()
                                                                                  .WithId()
                                                                                  .WithEpisode()
                                                                                  .WithAiringAt()
                                                                                  .WithMedia(MediaQueryBuilder()),
                                                                                  airingAtGreater: start,
                                                                                  airingAtLesser: end),
                                                             page).Build()
                });

                foreach (var item in response.Data.Page.AiringSchedules)
                {
                    if (item.AiringAt is null)
                    {
                        continue;
                    }

                    if (item.Media.MediaListEntry is null)
                    {
                        continue;
                    }

                    var startTime = DateTimeOffset.FromUnixTimeSeconds(item.AiringAt.Value).DateTime.ToLocalTime();
                    var anime = AniListModelToAnimeModelConverter.ConvertModel(item.Media);
                    anime.NextEpisodeAt = startTime;
                    if (item.Episode is { } ep)
                    {
                        anime.AiredEpisodes = ep - 1;
                    }


                    result.Add(new ScheduledAnime(anime)
                    {
                        Start = startTime
                    });
                }

                hasNextPage = response.Data.Page.PageInfo.HasNextPage ?? false;
                page++;
            } while (hasNextPage);
        }
        catch
        {
            return [];
        }

        return result;
    }

    private bool FilterNsfw(Media m)
    {
        if (settings.Value.IncludeNsfw)
        {
            return true;
        }

        return m.IsAdult is false or null;
    }

    private static MediaQueryBuilder MediaQueryBuilder()
    {
        return new MediaQueryBuilder()
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
               .WithDescription(false)
               .WithTrailer(new MediaTrailerQueryBuilder()
                            .WithSite()
                            .WithThumbnail()
                            .WithId())
               .WithGenres()
               .WithStartDate(new FuzzyDateQueryBuilder().WithAllFields())
               .WithEndDate(new FuzzyDateQueryBuilder().WithAllFields())
               .WithSeason()
               .WithSeasonYear()
               .WithBannerImage()
               .WithMediaListEntry(new MediaListQueryBuilder()
                                   .WithScore()
                                   .WithStatus()
                                   .WithStartedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithCompletedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithProgress())
               .WithIsAdult();
    }

    private static MediaQueryBuilder MediaQueryBuilderFull()
    {
        return new MediaQueryBuilder()
               .WithId()
               .WithIdMal()
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
               .WithDescription(false)
               .WithTrailer(new MediaTrailerQueryBuilder()
                            .WithSite()
                            .WithThumbnail()
                            .WithId())
               .WithGenres()
               .WithStartDate(new FuzzyDateQueryBuilder().WithAllFields())
               .WithEndDate(new FuzzyDateQueryBuilder().WithAllFields())
               .WithSeason()
               .WithSeasonYear()
               .WithBannerImage()
               .WithStudios(new StudioConnectionQueryBuilder()
                                .WithNodes(new StudioQueryBuilder()
                                           .WithName()
                                           .WithIsAnimationStudio()))
               .WithRelations(new MediaConnectionQueryBuilder()
                                  .WithNodes(MediaQueryBuilderSimple()))
               .WithRecommendations(new RecommendationConnectionQueryBuilder()
                                        .WithNodes(new RecommendationQueryBuilder()
                                                       .WithMediaRecommendation(MediaQueryBuilderSimple())))
               .WithStreamingEpisodes(new MediaStreamingEpisodeQueryBuilder()
                                      .WithTitle()
                                      .WithThumbnail())
               .WithAiringSchedule(new AiringScheduleConnectionQueryBuilder()
                                       .WithNodes(new AiringScheduleQueryBuilder()
                                                  .WithEpisode()
                                                  .WithAiringAt()))
               .WithMediaListEntry(new MediaListQueryBuilder()
                                   .WithScore()
                                   .WithStatus()
                                   .WithStartedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithCompletedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithProgress());
    }

    private static MediaQueryBuilder MediaQueryBuilderSimple()
    {
        return new MediaQueryBuilder()
               .WithId()
               .WithIdMal()
               .WithTitle(new MediaTitleQueryBuilder()
                          .WithEnglish()
                          .WithNative()
                          .WithRomaji())
               .WithDescription()
               .WithCoverImage(new MediaCoverImageQueryBuilder()
                                   .WithLarge())
               .WithType()
               .WithMediaListEntry(new MediaListQueryBuilder()
                                   .WithScore()
                                   .WithStatus()
                                   .WithStartedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithCompletedAt(new FuzzyDateQueryBuilder().WithAllFields())
                                   .WithProgress())
               .WithStatus();
    }
}