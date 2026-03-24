using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Threading;
using GraphQL.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class NextEpisodeTimeBehavior : AnimeCardOverlayBehavior<Border>
{
    private static readonly IAnimeMappingService MappingService = Container.Services.GetRequiredService<IAnimeMappingService>();
    private static readonly IAnimeExtensionService ExtensionService = Container.Services.GetRequiredService<IAnimeExtensionService>();
    private static readonly GraphQLHttpClient Client = Container.Services.GetRequiredService<GraphQLHttpClient>();

    private DateTime? _cachedAiringAt;
    private int _cachedCurrentEpisode;

    protected override Border CreateControl(AnimeModel anime)
    {
        return new Border()
               .Background(new SolidColorBrush(Colors.Black, 0.35))
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .VerticalAlignment(VerticalAlignment.Top)
               .Height(33)
               .Child(new TextBlock()
                      .Foreground(Brushes.White)
                      .Padding(2)
                      .HorizontalAlignment(HorizontalAlignment.Stretch)
                      .VerticalAlignment(VerticalAlignment.Center)
                      .FontSize(14)
                      .FontWeight(FontWeight.SemiBold)
                      .TextAlignment(TextAlignment.Center)
                      .TextTrimming(TextTrimming.CharacterEllipsis));
    }

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .Select(anime =>
                        {
                            RemoveControl();
                            _cachedAiringAt = null;
                            _cachedCurrentEpisode = 0;

                            return anime.WhenAnyValue(x => x.Tracking)
                                        .WhereNotNull()
                                        .ObserveOn(RxApp.MainThreadScheduler)
                                        .Select(_ => Observable.FromAsync(ct => FetchAndDisplayAiringTime(AssociatedObject!.Anime, ct)))
                                        .Switch();
                        })
                        .Switch()
                        .Subscribe()
                        .DisposeWith(Disposables);
    }

    protected override bool CanCreate(AnimeModel anime)
    {
        return anime.AiringStatus is not AiringStatus.FinishedAiring;
    }

    private async Task FetchAndDisplayAiringTime(AnimeModel anime, CancellationToken ct)
    {
        // Fetch the airing time once
        await FetchAiringTime(anime, ct);
        
        if (_cachedAiringAt is null)
        {
            Dispatcher.UIThread.Invoke(() => { Control?.IsVisible = false; });
            return;
        }

        EnsureControl(anime);
        UpdateDisplayText();

        // Set up a timer to refresh the UI every minute
        Observable.Interval(TimeSpan.FromMinutes(1), RxApp.MainThreadScheduler)
                  .Subscribe(_ => UpdateDisplayText())
                  .DisposeWith(Disposables);
    }

    private async Task FetchAiringTime(AnimeModel anime, CancellationToken ct)
    {
        if (anime.AiringStatus is not AiringStatus.CurrentlyAiring)
        {
            return;
        }

        var id = MappingService.GetId(anime);
        if (id is null)
        {
            return;
        }

        if (await ExtensionService.GetNextEpisodeAiringTimeAsync(anime, ct) is { } extAiringAt)
        {
            _cachedAiringAt = extAiringAt.DateTime;
        }
        else
        {
            (_, _cachedAiringAt) = await AnilistHelper.GetNextEpisodeInfo(Client, id.Anilist, ct);
        }

        _cachedCurrentEpisode = await AnilistHelper.GetTotalAiredEpisodes(Client, id.Anilist, ct);
    }

    private void UpdateDisplayText()
    {
        if (_cachedAiringAt is null)
        {
            return;
        }

        var remaining = _cachedAiringAt.Value - DateTime.Now;
        var time = remaining < TimeSpan.Zero
            ? "Aired"
            : $"EP{_cachedCurrentEpisode + 1}: {HumanizeTimeSpan(remaining)}";

        Dispatcher.UIThread.Invoke(() =>
        {
            if (Control?.Child is TextBlock textBlock)
            {
                textBlock.Text = time;
            }
        });
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