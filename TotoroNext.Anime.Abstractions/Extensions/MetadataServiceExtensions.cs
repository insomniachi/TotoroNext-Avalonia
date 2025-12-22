namespace TotoroNext.Anime.Abstractions.Extensions;

public static class MetadataServiceExtensions
{
    extension(IMetadataService service)
    {
        public async ValueTask<Models.AnimeModel?> FindAnimeAsync(Models.AnimeModel anime)
        {
            if (anime.ServiceId == service.Id)
            {
                return anime;
            }

            var response = await service.SearchAnimeAsync(anime.Title);

            return response switch
            {
                { Count: 0 } => null,
                { Count: 1 } => response[0],
                _ => response.FirstOrDefault(x => x.Season == anime.Season)
            };
        }
    }
}