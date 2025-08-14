namespace TotoroNext.Anime.AllAnime;

public static class GraphQl
{
    public const string Api = "https://api.allanime.day/api";

    public const string SearchQuery =
        """
        query( $search: SearchInput
               $limit: Int
               $page: Int
               $translationType: VaildTranslationTypeEnumType
               $countryOrigin: VaildCountryOriginEnumType )
        {
            shows( search: $search
                    limit: $limit
                    page: $page
                    translationType: $translationType
                    countryOrigin: $countryOrigin )
            {
                pageInfo
                {
                    total
                }
                edges 
                {
                    _id,
                    name,
                    availableEpisodesDetail,
                    season,
                    score,
                    thumbnail,
                    malId,
                    aniListId
                }
            }
        }
        """;

    public const string ShowQuery =
        """
        query ($showId: String!) {
            show(
                _id: $showId
            ) {
                availableEpisodesDetail,
                malId,
                aniListId
            }
        }
        """;

    public const string EpisodeQuery =
        """
        query ($showId: String!, $translationType: VaildTranslationTypeEnumType!, $episodeString: String!) {
            episode(
                showId: $showId
                translationType: $translationType
                episodeString: $episodeString
            ) {
                episodeString,
                sourceUrls,
                notes
            }
        }
        """;
}