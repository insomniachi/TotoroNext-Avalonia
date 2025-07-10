using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist;

internal class AnilistMetadataService(GraphQLHttpClient client,
                                      IModuleSettings<Settings> settings) : IMetadataService
{
    public async Task<List<AnimeModel>> GetAiringAnimeAsync()
    {
        var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
        {
            Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                .WithMedia(MediaQueryBuilder(), type: MediaType.Anime, status: MediaStatus.Releasing), page: 1, perPage: 20).Build()
        });

        if (response.Errors?.Length > 0)
        {
            return [];
        }

        return [.. response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel)];
    }

    public async Task<AnimeModel> GetAnimeAsync(long id)
    {
        var query = new QueryQueryBuilder().WithMedia(MediaQueryBuilderFull(), id: (int)id,
                                                                               type: MediaType.Anime).Build();

        var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
        {
            Query = query
        });

        return AniListModelToAnimeModelConverter.ConvertModel(response.Data.Media);
    }

    public async Task<List<AnimeModel>> GetSeasonalAnimeAsync()
    {
        var current = AnimeHelpers.CurrentSeason();
        var prev = AnimeHelpers.PrevSeason();
        var next = AnimeHelpers.NextSeason();

        List<AnimeModel> result = [];

        foreach (var season in new[] { current, prev, next })
        {
            var query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                        .WithMedia(MediaQueryBuilder(), season: AniListModelToAnimeModelConverter.ConvertSeason(season.SeasonName),
                                                        seasonYear: season.Year,
                                                        type: MediaType.Anime), 1, 50).Build();

            var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
            {
                Query = query
            });

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            result.AddRange(response.Data.Page.Media.Where(FilterNsfw).Select(AniListModelToAnimeModelConverter.ConvertModel));
        }

        return result;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQL.GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
            .WithMedia(MediaQueryBuilder(), search: term, type: MediaType.Anime), page: 1, perPage: (int)settings.Value.SearchLimit).Build()
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
            .WithDescription(asHtml: false)
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
            .WithDescription(asHtml: false)
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
