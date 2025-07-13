using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.MyAnimeList;

public static class MalToModelConverter
{
    public const string ServiceType = "MyAnimeList";

    public static AnimeModel ConvertModel(MalApi.Anime malModel)
    {
        var engTitle = malModel.AlternativeTitles?.English;

        var model = new AnimeModel
        {
            Id = malModel.Id,
            ExternalIds = new ExternalIds
            {
                MyAnimeList = malModel.Id
            },
            Title = malModel.Title,
            EngTitle = string.IsNullOrEmpty(engTitle) ? malModel.Title : engTitle,
            RomajiTitle = malModel.Title,
            Image = malModel.MainPicture?.Large ?? string.Empty,
            ServiceType = ServiceType,
            Description = malModel.Synopsis ?? string.Empty,
        };

        try
        {
            if (malModel.UserStatus is { } progress)
            {
                model.Tracking = new Tracking
                {
                    WatchedEpisodes = progress.WatchedEpisodes,
                    Status = progress.IsRewatching ? ListItemStatus.Rewatching : (ListItemStatus)(int)progress.Status,
                    Score = (int)progress.Score,
                    UpdatedAt = progress.UpdatedAt,
                    StartDate = progress.StartDate,
                    FinishDate = progress.FinishDate
                };
            }
            model.AiringStatus = (AiringStatus)(int)(malModel.Status ?? MalApi.AiringStatus.NotYetAired);
            model.TotalEpisodes = malModel.TotalEpisodes;
            model.MeanScore = malModel.MeanScore;
            model.Popularity = malModel.Popularity ?? 0;
            //if (DateTime.TryParseExact(malModel.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            //{
            //    model.BroadcastDay = dt.DayOfWeek;
            //}

            //if (malModel.AlternativeTitles is { } alt)
            //{
            //    var titles = alt.Aliases.ToList();
            //    titles.Add(alt.English);
            //    titles.Add(alt.Japanese);
            //    model.AlternativeTitles = titles.Distinct();
            //}

            //if (malModel.Videos is { Length: > 0 } videos)
            //{
            //    model.Videos = malModel.Videos.Select(x => new Video
            //    {
            //        Id = x.Id,
            //        Thumbnail = x.Thumbnail,
            //        Title = x.Title,
            //        Url = x.Url,
            //    }).ToList();
            //}

            if (malModel.StartSeason is { } season)
            {
                model.Season = new((AnimeSeason)(int)season.SeasonName, season.Year);
            }

            //if (malModel.Genres is { Length: > 0 } g)
            //{
            //    model.Genres = g.Select(x => x.Name).ToArray();
            //}

            if (malModel.RelatedAnime is { Length: > 0 } ra)
            {
                model.Related = ra.Select(x => ConvertModel(x.Anime)).ToArray();
            }

            if (malModel.Recommendations is { Length: > 0 } rec)
            {
                model.Recommended = rec.Select(x => ConvertModel(x.Anime)).ToArray();
            }
        }
        catch
        {
            //Locator.Current.GetService<ILogManager>().GetLogger<MalToModelConverter>().Error(ex);
        }

        return model;
    }
}
