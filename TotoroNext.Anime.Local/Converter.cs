using System.Reflection;
using System.Runtime.Serialization;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Local;

internal static class Converter
{
    internal static OfflineAnimeModel ToDbModel(AnimeModelRemote model)
    {
        return new OfflineAnimeModel
        {
            AnilistId = model.Id,
            MyAnimeListId = model.ExternalIds.MyAnimeList,
            AniDbId = model.ExternalIds.Anidb,
            AnnId = model.ExternalIds.AnimeNewsNetwork,
            KitsuId = model.ExternalIds.Kitsu,
            SimklId = model.ExternalIds.Simkl,
            Title = model.Title,
            TotalEpisodes = model.TotalEpisodes ?? 0,
            Season = ConvertSeasonToDbModel(model),
            MeanScore = model.MeanScore,
            Image = model.CoverImage ?? "",
            Thumbnail = model.BannerImage ?? "",
            Genres = [.. model.Genres],
            Studios = [.. model.SupportingStudios],
            AiringStatus = ConvertToDbStatus(model.Status),
            MediaFormat = ConvertToDbMediaFormat(model.Format),
            Related = [.. model.Relations],
            Description = model.Description,
            StartDate = model.StartDate,
            EndDate = model.EndDate
        };
    }

    internal static OfflineAnimeModel ToDbModel(Media model)
    {
        var result = new OfflineAnimeModel()
        {
            AnilistId = model.Id ?? 0,
            MyAnimeListId = model.IdMal,
            Title = new AnimeTitle()
            {
                English = model.Title.English,
                Native = model.Title.Native,
                Romaji = model.Title.Romaji
            },
            TotalEpisodes = model.Episodes ?? 0,
            Season = ConvertSeasonToDbModel(model),
            MeanScore = model.MeanScore ?? 0,
            Image = model.CoverImage.ExtraLarge,
            Thumbnail = model.BannerImage,
            Genres = [.. model.Genres],
            Studios = [..model.Studios.Edges.Select(x => x.Node.Name)],
            AiringStatus = ConvertToDbStatus(model.Status),
            MediaFormat = ConvertToDbMediaFormat(model.Format),
            Related = [..model.Relations.Edges.Select(x => new AnimeRelationship()
            {
                RelationType = GetEnumMemberValue(x.RelationType!.Value),
                Id = x.Node.Id ?? 0
            })],
            Description = model.Description,
            StartDate = ConvertDate(model.StartDate),
            EndDate = ConvertDate(model.EndDate),
        };

        return result;
    }

    private static DateOnly? ConvertDate(FuzzyDate date)
    {
        if (date.Day is null || date.Month is null || date.Year is null)
        {
            return null;
        }

        return new DateOnly(date.Year.Value, date.Month.Value, date.Day.Value);
    }

    public static AnimeModel ToAppModel(OfflineAnimeModel anime)
    {
        var model = new AnimeModel
        {
            Title = anime.Title.Romaji ?? "",
            TotalEpisodes = anime.TotalEpisodes,
            Genres = [.. anime.Genres],
            Season = anime.Season,
            MeanScore = anime.MeanScore,
            Studios = anime.Studios,
            AiringStatus = anime.AiringStatus,
            Image = anime.Image,
            BannerImage = anime.Thumbnail,
            ServiceName = "Local",
            ServiceId = Module.Id,
            Id = anime.AnilistId,
            ExternalIds = new AnimeId
            {
                Anilist = anime.AnilistId,
                MyAnimeList = anime.MyAnimeListId ?? 0,
                Kitsu = anime.KitsuId ?? 0,
                AniDb = anime.AniDbId ?? 0,
                Simkl = anime.SimklId ?? 0,
                AnimeNewsNetwork = anime.AnnId ?? 0
            },
            EngTitle = anime.Title.English ?? anime.Title.Romaji ?? "",
            RomajiTitle = anime.Title.Romaji ?? "",
            Description = anime.Description,
            Tracking = anime.Tracking?.Tracking,
            Url = $"https://myanimelist.net/anime/{anime.MyAnimeListId}/",
            MediaFormat = anime.MediaFormat,
            AlternateTitles = ConvertTitlesToDbModel(anime.Title),
            Trailers = anime.Trailer is null
                ? new List<TrailerVideo>()
                :
                [
                    new TrailerVideo
                    {
                        Url = anime.Trailer!.Url,
                        Thumbnail = anime.Thumbnail,
                        Title = "Trailer"
                    }
                ]
        };

        return model;
    }

    internal static List<string> ConvertTitlesToDbModel(AnimeTitle title)
    {
        return [.. new[] { title.English, title.Native }.OfType<string>().Select(x => x)];
    }

    internal static Season? ConvertSeasonToDbModel(AnimeModelRemote model)
    {
        if (string.IsNullOrEmpty(model.Season) || model.SeasonYear == 0)
        {
            return null;
        }

        AnimeSeason? seasonName = model.Season switch
        {
            "SUMMER" => AnimeSeason.Summer,
            "WINTER" => AnimeSeason.Winter,
            "SPRING" => AnimeSeason.Spring,
            "FALL" => AnimeSeason.Fall,
            _ => null
        };

        return seasonName is null ? null : new Season(seasonName.Value, model.SeasonYear);
    }
    
    internal static Season? ConvertSeasonToDbModel(Media model)
    {
        if (model.Season is null || model.SeasonYear is not > 0)
        {
            return null;
        }

        AnimeSeason? seasonName = model.Season switch
        {
            MediaSeason.Summer => AnimeSeason.Summer,
            MediaSeason.Winter => AnimeSeason.Winter,
            MediaSeason.Spring => AnimeSeason.Spring,
            MediaSeason.Fall => AnimeSeason.Fall,
            _ => null
        };

        return seasonName is null ? null : new Season(seasonName.Value, model.SeasonYear.Value);
    }

    internal static AnimeMediaFormat ConvertToDbMediaFormat(string format)
    {
        return format switch
        {
            "TV" or "TV_SHORT" => AnimeMediaFormat.Tv,
            "MOVIE" => AnimeMediaFormat.Movie,
            "SPECIAL" => AnimeMediaFormat.Special,
            "OVA" => AnimeMediaFormat.Ova,
            "ONA" => AnimeMediaFormat.Ona,
            "MUSIC" => AnimeMediaFormat.Music,
            _ => AnimeMediaFormat.Unknown
        };
    }
    
    internal static AnimeMediaFormat ConvertToDbMediaFormat(MediaFormat? format)
    {
        return format switch
        {
            MediaFormat.Tv or MediaFormat.TvShort => AnimeMediaFormat.Tv,
            MediaFormat.Movie => AnimeMediaFormat.Movie,
            MediaFormat.Special => AnimeMediaFormat.Special,
            MediaFormat.Ova => AnimeMediaFormat.Ova,
            MediaFormat.Ona => AnimeMediaFormat.Ona,
            MediaFormat.Music => AnimeMediaFormat.Music,
            _ => AnimeMediaFormat.Unknown
        };
    }

    internal static AiringStatus ConvertToDbStatus(string status)
    {
        return status switch
        {
            "FINISHED" => AiringStatus.FinishedAiring,
            "RELEASING" => AiringStatus.CurrentlyAiring,
            _ => AiringStatus.NotYetAired
        };
    }
    
    internal static AiringStatus ConvertToDbStatus(MediaStatus? status)
    {
        return status switch
        {
            MediaStatus.Finished => AiringStatus.FinishedAiring,
            MediaStatus.Releasing => AiringStatus.CurrentlyAiring,
            _ => AiringStatus.NotYetAired
        };
    }
    
    public static string GetEnumMemberValue<T>(T enumValue) where T : Enum
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString());
        
        if (memberInfo.Length > 0)
        {
            var attribute = memberInfo[0].GetCustomAttribute<EnumMemberAttribute>();
            if (attribute != null)
            {
                return attribute.Value ?? enumValue.ToString();
            }
        }
        
        return enumValue.ToString(); // Fallback to string name if no attribute exists
    }
}