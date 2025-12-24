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
    private static readonly GraphQLHttpClient Client = Container.Services.GetRequiredService<GraphQLHttpClient>();
    
    protected override Border CreateControl()
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

                            return anime.WhenAnyValue(x => x.Tracking)
                                        .WhereNotNull()
                                        .ObserveOn(RxApp.MainThreadScheduler)
                                        .Select(_ => Observable.FromAsync(ct => UpdateAiringTime(AssociatedObject!, AssociatedObject!.Anime, ct)))
                                        .Switch();
                        })
                        .Switch()
                        .Subscribe()
                        .DisposeWith(Disposables);
    }

    private async Task UpdateAiringTime(AnimeCard card, AnimeModel anime, CancellationToken ct)
    {
        var time = await ToNextEpisodeAiringTime(anime, ct);
        if (string.IsNullOrEmpty(time))
        {
            Dispatcher.UIThread.Invoke(() => { Control?.IsVisible = false; });

            return;
        }

        EnsureControl();

        Dispatcher.UIThread.Invoke(() =>
        {
            var textBlock = (TextBlock)Control!.Child!;
            textBlock.Text = time;
        });
    }

    private static async Task<string> ToNextEpisodeAiringTime(AnimeModel? anime, CancellationToken ct)
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

            (current, airingAt) = await AnilistHelper.GetNextEpisodeInfo(Client, id.Anilist, ct);
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