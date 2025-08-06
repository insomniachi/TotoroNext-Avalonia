using System.Reactive;
using System.Text;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Behaviors;

public class UnwatchedEpisodesBehavior : Behavior<AnimeCard>
{
    private static readonly SolidColorBrush NotUploadedBrush = new(Colors.Orange);
    private static readonly IAnimeOverridesRepository Overrides = Container.Services.GetRequiredService<IAnimeOverridesRepository>();
    private static readonly IFactory<IAnimeProvider, Guid> ProviderFactory = Container.Services.GetRequiredService<IFactory<IAnimeProvider, Guid>>();

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        UpdateAiringTime(AssociatedObject, AssociatedObject.Anime);
        _ = UpdateBadge(AssociatedObject.Anime);
    }

    private async Task<Unit> UpdateBadge(AnimeModel anime)
    {
        if (AssociatedObject is null ||
            anime.AiringStatus is not AiringStatus.CurrentlyAiring ||
            anime.Tracking?.Status is not (ListItemStatus.Watching or ListItemStatus.PlanToWatch))
        {
            return Unit.Default;
        }

        var overrides = Overrides.GetOverrides(anime.Id);
        var provider = overrides?.Provider is not { } id
            ? ProviderFactory.CreateDefault()
            : ProviderFactory.Create(id);

        var title = overrides?.SelectedResult ?? anime.Title;
        var result = await provider.SearchAndSelectAsync(title);

        if (result is null)
        {
            return Unit.Default;
        }

        var watched = anime.Tracking?.WatchedEpisodes ?? 0;
        var total = anime.AiredEpisodes;
        var diff = total - watched;

        if (diff == 0)
        {
            return Unit.Default;
        }

        var episodes = await result.GetEpisodes().ToListAsync();
        var actuallyAired = (int)episodes.Max(x => x.Number);
        var actualDiff = actuallyAired - watched;

        // Episode aired on TV, but not uploaded on the provider
        if (actualDiff == 0)
        {
            AssociatedObject.Badge.Background = NotUploadedBrush;
            AssociatedObject.BadgeText.Text = diff.ToString();

            return Unit.Default;
        }

        AssociatedObject.BadgeText.Text = actualDiff.ToString();

        return Unit.Default;
    }

    private static void UpdateAiringTime(AnimeCard card, AnimeModel anime)
    {
        var time = ToNextEpisodeAiringTime(anime);
        if (string.IsNullOrEmpty(time))
        {
            return;
        }

        card.NextEpText.IsVisible = true;
        card.NextEpText.Text = time;
    }

    private static string ToNextEpisodeAiringTime(AnimeModel? anime)
    {
        var airingAt = anime?.NextEpisodeAt;
        var current = anime?.AiredEpisodes;

        return airingAt is null
            ? string.Empty
            : $"EP{current + 1}: {HumanizeTimeSpan(airingAt.Value - DateTime.Now)}";
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