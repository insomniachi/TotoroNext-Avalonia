using LiteDB;

namespace TotoroNext.Anime.Local;

internal static class LiteDbCollectionExtensions
{
    extension(ILiteCollection<OfflineAnimeModel> collection)
    {
        internal ILiteCollection<OfflineAnimeModel> IncludeExtras()
        {
            return collection.Include(x => x.Tracking);
        }
    }
}