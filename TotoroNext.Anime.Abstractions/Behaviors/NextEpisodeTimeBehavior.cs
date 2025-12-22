using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using GraphQL.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class NextEpisodeTimeBehavior : Behavior<AnimeCard>, IControlAttachingBehavior
{
    private static readonly IAnimeMappingService MappingService = Container.Services.GetRequiredService<IAnimeMappingService>();
    private static readonly GraphQLHttpClient Client = Container.Services.GetRequiredService<GraphQLHttpClient>();

    private readonly CompositeDisposable _disposable = new();
    private Border? _control;

    public void OnHoverEntered()
    {
        _control?.IsVisible = false;
    }

    public void OnHoverExited()
    {
        _control?.IsVisible = true;
    }

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .Select(anime =>
                        {
                            if (_control is not null)
                            {
                                AssociatedObject?.ImageContainer.Children.Remove(_control);
                                _control = null;
                            }

                            return anime.WhenAnyValue(x => x.Tracking)
                                        .WhereNotNull()
                                        .ObserveOn(RxApp.MainThreadScheduler)
                                        .Select(_ => Observable.FromAsync(ct => UpdateAiringTime(AssociatedObject!, AssociatedObject!.Anime, ct)))
                                        .Switch();
                        })
                        .Switch()
                        .Subscribe()
                        .DisposeWith(_disposable);
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (_control is not null)
        {
            AssociatedObject?.ImageContainer.Children.Remove(_control);
            _control = null;
        }

        _disposable.Dispose();
    }

    private async Task UpdateAiringTime(AnimeCard card, AnimeModel anime, CancellationToken ct)
    {
        var time = await ToNextEpisodeAiringTime(anime, ct);
        if (string.IsNullOrEmpty(time))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                card.NextEpText.IsVisible = false;
                _control?.IsVisible = false;
            });

            return;
        }

        EnsureControl();

        Dispatcher.UIThread.Invoke(() =>
        {
            var textBlock = (TextBlock)_control!.Child!;
            textBlock.Text = time;
            card.NextEpText.Text = time;
            card.NextEpText.IsVisible = true;
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

    private void EnsureControl()
    {
        if (_control is not null && _control.Parent == AssociatedObject?.ImageContainer)
        {
            return;
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            _control = CreatControl();
            AssociatedObject?.ImageContainer.Children.Add(_control);
        });
    }

    private static Border CreatControl()
    {
        return new Border
        {
            Background = new SolidColorBrush { Color = Colors.Black, Opacity = 0.35 },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Height = 33,
            Child = new TextBlock
            {
                Foreground = Brushes.White,
                Padding = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
    }
}