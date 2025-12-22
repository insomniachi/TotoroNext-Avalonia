using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using IconPacks.Avalonia;
using IconPacks.Avalonia.BootstrapIcons;
using IconPacks.Avalonia.MaterialDesign;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Behaviors;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Behaviors;

public class InteractiveCardControlsBehavior : Behavior<AnimeCard>, IControlAttachingBehavior
{
    private static readonly SolidColorBrush StatusBorderBrush = new(Color.Parse("#AA000000"));
    private readonly CompositeDisposable _disposables = new();
    private Grid? _control;

    public void OnHoverEntered()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.StatusBorder.Height = 300;
        AssociatedObject.StatusBorder.Background = Brushes.Transparent;
        AssociatedObject.TitleBorder.Height = double.NaN;
        AssociatedObject.TitleBorder.MaxHeight = 120;
        AssociatedObject.TitleTextBlock.FontWeight = FontWeight.Bold;
        AssociatedObject.TitleTextBlock.FontSize = 18;
        AssociatedObject.TitleTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;
        AssociatedObject.TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        AssociatedObject.Tint.IsVisible = true;
        if (AssociatedObject.ImageContainer.Effect is BlurEffect effect)
        {
            effect.Radius = 25;
        }
    }

    public void OnHoverExited()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.StatusBorder.Height = 60;
        AssociatedObject.StatusBorder.Background = StatusBorderBrush;
        AssociatedObject.TitleBorder.Height = 54;
        AssociatedObject.TitleTextBlock.FontWeight = FontWeight.Normal;
        AssociatedObject.TitleTextBlock.FontSize = 15;
        AssociatedObject.TitleTextBlock.TextWrapping = TextWrapping.NoWrap;
        AssociatedObject.TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        AssociatedObject.Tint.IsVisible = false;
        if (AssociatedObject.ImageContainer.Effect is BlurEffect effect)
        {
            effect.Radius = 0;
        }
    }

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(anime =>
                        {
                            RemoveControl();
                            EnsureControl(anime);
                        })
                        .DisposeWith(_disposables);
    }

    protected override void OnDetachedFromVisualTree()
    {
        RemoveControl();
        _disposables.Dispose();
    }

    private void RemoveControl()
    {
        if (_control is null)
        {
            return;
        }

        AssociatedObject?.ContentGrid.Children.Remove(_control);
        _control = null;
    }

    private void EnsureControl(AnimeModel anime)
    {
        if (_control is not null)
        {
            return;
        }

        try
        {
            _control = CreateControl(anime);
            AssociatedObject?.ContentGrid.Children.Add(_control);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static Grid CreateControl(AnimeModel anime)
    {
        var grid = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("*,Auto"),
            Children =
            {
                CreateContent(anime),
                CreateFooter(anime)
            }
        };

        Grid.SetRow(grid, 1);
        return grid;
    }

    private static StackPanel CreateContent(AnimeModel anime)
    {
        return new StackPanel()
               .Spacing(4)
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .Margin(8)
               .Children(WatchButton(anime), TorrentButton(anime), DetailsButton(anime))
               .Row(0);
    }

    private static Border CreateFooter(AnimeModel anime)
    {
        var border = new Border
        {
            Padding = new Thickness(4),
            Child = new Grid
            {
                Children =
                {
                    new StackPanel()
                        .Orientation(Orientation.Horizontal)
                        .Spacing(8)
                        .Margin(4)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .Children(EditButton(anime),
                                  AddToListButton(anime),
                                  CreateMeanScoreBorder(anime)),
                    new StackPanel()
                        .Orientation(Orientation.Horizontal)
                        .HorizontalAlignment(HorizontalAlignment.Right)
                        .Spacing(8)
                        .Margin(4)
                        .Children(DownloadButton(anime),
                                  CreateSettingsButton(anime))
                }
            }
        };

        Grid.SetRow(border, 1);

        return border;
    }
    
    private static Button WatchButton(AnimeModel anime)
    {
        return new Button()
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .Command(Commands.WatchCommand)
               .CommandParameter(anime)
               .IsVisible(anime.AiringStatus != AiringStatus.NotYetAired)
               .Content(new StackPanel()
                        .Spacing(8)
                        .Orientation(Orientation.Horizontal)
                        .Children(new TextBlock().Text("Watch").VerticalAlignment(VerticalAlignment.Center),
                                  new PackIconControl { Kind = PackIconBootstrapIconsKind.Play }
                                      .Height(15).Width(11)
                                      .VerticalAlignment(VerticalAlignment.Center)));
    }

    private static Button TorrentButton(AnimeModel anime)
    {
        return new Button()
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .Command(Commands.SearchTorrentsCommand)
               .CommandParameter(anime)
               .IsVisible(anime.AiringStatus != AiringStatus.NotYetAired)
               .Content(new StackPanel()
                        .Spacing(8)
                        .Orientation(Orientation.Horizontal)
                        .Children(new TextBlock().Text("Torrent").VerticalAlignment(VerticalAlignment.Center),
                                  new PackIconControl { Kind = PackIconBootstrapIconsKind.Play }
                                      .Height(15).Width(11)
                                      .VerticalAlignment(VerticalAlignment.Center)));
    }

    private static Button DetailsButton(AnimeModel anime)
    {
        return new Button()
               .Content("Details")
               .Command(Commands.DetailsCommand)
               .CommandParameter(anime)
               .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private static Button EditButton(AnimeModel anime)
    {
        var button = new Button()
                     .CornerRadius(30)
                     .Height(30).Width(30)
                     .OnClick(_ =>
                     {
                         WeakReferenceMessenger.Default.Send(new NavigateToKeyDialogMessage
                         {
                             Title = anime.Title,
                             Key = $"tracking/{anime.ServiceName}",
                             Button = DialogButton.OKCancel,
                             Data = anime
                         });
                     })
                     .Content(new Viewbox()
                              .Height(12).Width(12)
                              .Child(new PackIconControl { Kind = PackIconMaterialDesignKind.Edit }));

        button.Bind(Visual.IsVisibleProperty, new Binding("Tracking")
        {
            Source = anime,
            Converter = ObjectConverters.IsNotNull
        });

        return button;
    }
    
    private static Button AddToListButton(AnimeModel anime)
    {
        var button = new Button()
                     .Command(Commands.AddToListCommand)
                     .CommandParameter(anime)
                     .CornerRadius(30)
                     .Height(30).Width(30)
                     .Content(new Viewbox()
                              .Height(12).Width(12)
                              .Child(new PackIconControl { Kind = PackIconMaterialDesignKind.Add }));

        button.Bind(Visual.IsVisibleProperty, new Binding("Tracking")
        {
            Source = anime,
            Converter = ObjectConverters.IsNull
        });

        return button;
    }

    private static Border CreateMeanScoreBorder(AnimeModel anime)
    {
        return new Border()
               .Background(new DynamicResourceExtension("ButtonDefaultBackground"))
               .BorderBrush(new DynamicResourceExtension("ButtonDefaultBorderBrush"))
               .BorderThickness(new DynamicResourceExtension("ButtonBorderThickness"))
               .CornerRadius(20)
               .Padding(8, 5)
               .Child(new StackPanel()
                      .Spacing(4)
                      .Orientation(Orientation.Horizontal)
                      .Children(new PackIconControl { Kind = PackIconMaterialDesignKind.Star }
                                .Foreground(new DynamicResourceExtension("ButtonDefaultPrimaryForeground"))
                                .Height(12).Width(12)
                                .VerticalAlignment(VerticalAlignment.Center).HorizontalAlignment(HorizontalAlignment.Center),
                                new TextBlock()
                                    .Text($"{anime.MeanScore}")
                                    .Foreground(new DynamicResourceExtension("ButtonDefaultPrimaryForeground"))
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .FontSize(13)
                                    .FontWeight(FontWeight.Bold)));
    }

    private static Button DownloadButton(AnimeModel anime)
    {
        return new Button()
               .CornerRadius(30)
               .Height(30).Width(30)
               .OnClick(_ =>
               {
                   WeakReferenceMessenger.Default.Send(new NavigateToKeyDialogMessage
                   {
                       Title = anime.Title,
                       Data = anime,
                       Key = "Download",
                       Button = DialogButton.OKCancel
                   });
               })
               .Content(new Viewbox()
                        .Height(12).Width(12)
                        .Child(new PackIconControl { Kind = PackIconMaterialDesignKind.Download }));
    }

    private static Button CreateSettingsButton(AnimeModel anime)
    {
        return new Button()
               .Command(Commands.SettingsCommand)
               .CommandParameter(anime)
               .CornerRadius(30)
               .Height(30).Width(30)
               .Content(new Viewbox()
                        .Height(12).Width(12)
                        .Child(new PackIconControl { Kind = PackIconMaterialDesignKind.Settings }));
    }
}