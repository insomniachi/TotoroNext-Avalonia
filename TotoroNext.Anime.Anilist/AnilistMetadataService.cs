using GraphQL;
using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist;

internal class AnilistMetadataService(
    GraphQLHttpClient client,
    IModuleSettings<Settings> settings) : IMetadataService
{
    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        return await anime.GetEpisodes();
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
                                                                        seasonYear: request.Year,
                                                                        source: AniListModelToAnimeModelConverter.ConvertSource(request.Source),
                                                                        genreIn: request.IncludedGenres,
                                                                        genreNotIn: request.ExcludedGenres,
                                                                        averageScoreGreater: request.MinimumScore,
                                                                        averageScoreLesser: request.MaximumScore,
                                                                        sort: new List<MediaSort?> { MediaSort.ScoreDesc },
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

    public Guid Id { get; } = Module.Id;

    public string Name { get; } = nameof(ExternalIds.Anilist);

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

    public async Task<List<AnimeModel>> GetAiringAnimeAsync()
    {
        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                         .WithMedia(MediaQueryBuilder(), type: MediaType.Anime, status: MediaStatus.Releasing), 1, 20)
                                           .Build()
        });

        if (response.Errors?.Length > 0)
        {
            return [];
        }

        return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
    }

    public async Task<List<AnimeModel>> GetAnimeAsync(Season season)
    {
        var query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                         .WithMedia(MediaQueryBuilder(),
                                                                    season: AniListModelToAnimeModelConverter.ConvertSeason(season.SeasonName),
                                                                    seasonYear: season.Year,
                                                                    type: MediaType.Anime), 1, 50).Build();

        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        if (response.Errors?.Length > 0)
        {
            return [];
        }

        return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
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