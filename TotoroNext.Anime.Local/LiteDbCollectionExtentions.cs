using LiteDB;

namespace TotoroNext.Anime.Local;

internal static class LiteDbCollectionExtensions
{
    extension(ILiteCollection<LocalAnimeModel> collection)
    {
        internal ILiteCollection<LocalAnimeModel> IncludeExtras()
        {
            return collection.Include(x => x.Tracking)
                             .Include(x => x.EpisodeInfo)
                             .Include(x => x.CharacterInfo)
                             .Include(x => x.AdditionalInfo);
        }
    }
}