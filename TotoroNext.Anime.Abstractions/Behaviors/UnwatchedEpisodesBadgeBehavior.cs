using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using GraphQL.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class UnwatchedEpisodesBadgeBehavior : Behavior<AnimeCard>, IControlAttachingBehavior
{
    private static readonly IAnimeExtensionService ExtensionService = Container.Services.GetRequiredService<IAnimeExtensionService>();
    private static readonly IAnimeRelations Relations = Container.Services.GetRequiredService<IAnimeRelations>();
    private static readonly IAnimeMappingService MappingService = Container.Services.GetRequiredService<IAnimeMappingService>();
    private readonly CompositeDisposable _disposable = new();
    private Border? _control;
    private static GraphQLHttpClient Client => Container.Services.GetRequiredService<GraphQLHttpClient>();

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
                                        .Select(_ => Observable.FromAsync(ct => UpdateBadge(AssociatedObject!.Anime, ct)))
                                        .Switch();
                        })
                        .Switch()
                        .Subscribe()
                        .DisposeWith(_disposable);
    }

    protected override void OnDetachedFromLogicalTree()
    {
        if (_control is not null)
        {
            AssociatedObject?.ImageContainer.Children.Remove(_control);
            _control = null;
        }

        _disposable.Dispose();
    }


    private async Task UpdateBadge(AnimeModel? anime, CancellationToken ct)
    {
        if (anime is null ||
            anime.AiringStatus is AiringStatus.NotYetAired ||
            anime.Tracking?.Status is not (ListItemStatus.Watching or ListItemStatus.PlanToWatch) ||
            (anime.Season != AnimeHelpers.CurrentSeason() && anime.AiringStatus != AiringStatus.CurrentlyAiring))
        {
            return;
        }

        var watched = anime.Tracking?.WatchedEpisodes ?? 0;
        var total = anime.AiredEpisodes;
        if (total == 0 && MappingService.GetId(anime) is { } id)
        {
            total = await AnilistHelper.GetTotalAiredEpisodes(Client, id.Anilist, ct);
        }

        var diff = total - watched;

        if (diff <= 0)
        {
            _control?.IsVisible = false;
            return;
        }

        EnsureControl();

        _control?.Background = Brushes.Orange;
        (_control?.Child as TextBlock)?.Text = diff.ToString();

        var result = await ExtensionService.SearchAsync(anime);

        if (result is null)
        {
            return;
        }

        var episodes = await result.GetEpisodes(ct);
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
            return;
        }

        _control?.Background = Brushes.Red;
        (_control?.Child as TextBlock)?.Text = actualDiff.ToString();
    }

    public void EnsureControl()
    {
        if (_control is not null)
        {
            return;
        }

        _control = CreateControl();
        AssociatedObject?.ImageContainer.Children.Add(_control);
    }


    private static Border CreateControl()
    {
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(3),
            Margin = new Thickness(4),
            Width = 30,
            Child = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold
            }
        };
    }
}