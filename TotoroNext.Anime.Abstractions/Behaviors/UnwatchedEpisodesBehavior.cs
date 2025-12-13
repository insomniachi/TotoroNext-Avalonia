using System.Reactive;
using System.Text;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class UnwatchedEpisodesBehavior : Behavior<AnimeCard>, IVirtualizingBehavior<AnimeCard>
{
    private static readonly SolidColorBrush NotUploadedBrush = new(Colors.Orange);
    private static readonly SolidColorBrush UploadedBrush = new(Colors.Red);
    private static readonly IAnimeExtensionService ExtensionService = Container.Services.GetRequiredService<IAnimeExtensionService>();
    private static readonly IAnimeRelations Relations = Container.Services.GetRequiredService<IAnimeRelations>();
    private static readonly IAnimeMappingService MappingService = Container.Services.GetRequiredService<IAnimeMappingService>();
    private static readonly Lazy<GraphQLHttpClient> ClientLazy =
        new(new GraphQLHttpClient("https://graphql.anilist.co/", new NewtonsoftJsonSerializer(), new HttpClient()));

    public void Update(AnimeCard card)
    {
        _ = UpdateAiringTime(card, card.Anime);
        _ = UpdateBadge(card, card.Anime);
    }

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        Update(AssociatedObject);
    }

    private static async Task<Unit> UpdateBadge(AnimeCard card, AnimeModel anime)
    {
        if (anime.AiringStatus is AiringStatus.NotYetAired ||
            anime.Tracking?.Status is not (ListItemStatus.Watching or ListItemStatus.PlanToWatch) ||
            anime.Season != AnimeHelpers.CurrentSeason())
        {
            return Unit.Default;
        }

        var watched = anime.Tracking?.WatchedEpisodes ?? 0;
        var total = anime.AiredEpisodes;
        if (total == 0  && MappingService.GetId(anime) is {} id)
        {
            total = await AnilistHelper.GetTotalAiredEpisodes(ClientLazy.Value, id.Anilist);
        }
        var diff = total - watched;

        if (diff <= 0)
        {
            return Unit.Default;
        }

        card.Badge.Background = NotUploadedBrush;
        card.BadgeText.Text = diff.ToString();

        var result = await ExtensionService.SearchAsync(anime);

        if (result is null)
        {
            return Unit.Default;
        }

        var episodes = await result.GetEpisodes(CancellationToken.None);
        if (episodes.Count > (anime.TotalEpisodes ?? 0) && Relations.FindRelation(anime) is { } relation)
        {
            episodes = episodes.Where(x => x.Number >= relation.SourceEpisodesRage.Start && x.Number <= relation.SourceEpisodesRage.End).ToList();
            foreach (var ep in episodes)
            {
                ep.Number -= relation.SourceEpisodesRage.Start - 1;
            }
        }

        var actuallyAired = (int)episodes.Max(x => x.Number);
        var actualDiff = actuallyAired - watched;

        // Episode aired on TV, but not uploaded on the provider
        if (actualDiff <= 0)
        {
            card.Badge.Background = NotUploadedBrush;
            card.BadgeText.Text = diff.ToString();

            return Unit.Default;
        }

        card.Badge.Background = UploadedBrush;
        card.BadgeText.Text = actualDiff.ToString();

        return Unit.Default;
    }

    private static async Task UpdateAiringTime(AnimeCard card, AnimeModel anime)
    {
        var time = await ToNextEpisodeAiringTime(anime);
        if (string.IsNullOrEmpty(time))
        {
            card.NextEpText.IsVisible = false;
            return;
        }

        card.NextEpText.IsVisible = true;
        card.NextEpText.Text = time;
    }

    private static async Task<string> ToNextEpisodeAiringTime(AnimeModel? anime)
    {
        if (anime is null)
        {
            return string.Empty;
        }
        
        var airingAt = anime.NextEpisodeAt;
        var current = anime.AiredEpisodes;
       
        if (airingAt is null && 
            anime.AiringStatus is AiringStatus.CurrentlyAiring &&
            anime.ServiceName != nameof(AnimeId.Anilist))
        {
            var id = MappingService.GetId(anime);
            if (id is null)
            {
                return string.Empty;
            }
            (current, airingAt) = await AnilistHelper.GetNextEpisodeInfo(ClientLazy.Value, id.Anilist);
            current--;
        }

        if (airingAt is null)
        {
            return string.Empty;
        }

        var remaining = airingAt.Value - DateTime.Now;

        return remaining < TimeSpan.Zero
            ? "Aired"
            : $"EP{current + 1}: {HumanizeTimeSpan(remaining)}";
    }

    private static string HumanizeTimeSpan(TimeSpan ts)
    {
        var sb = new StringBuilder();
        var week = ts.Days / 7;
        var days = ts.Days % 7;
        if (week > 0)
        {
            sb.Append($"{week}w ");
        }

        if (days > 0)
        {
            sb.Append($"{days}d ");
        }

        if (ts.Hours > 0)
        {
            sb.Append($"{ts.Hours.ToString().PadLeft(2, '0')}h ");
        }

        if (ts.Minutes > 0)
        {
            sb.Append($"{ts.Minutes.ToString().PadLeft(2, '0')}m ");
        }

        return sb.ToString().TrimEnd();
    }
}