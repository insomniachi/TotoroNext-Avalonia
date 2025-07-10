using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TotoroNext.Anime.Abstractions.Controls;

public partial class AnimeCard : UserControl
{
    public AnimeCard()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<AnimeModel> AnimeProperty = AvaloniaProperty.Register<AnimeCard, AnimeModel>(nameof(Anime));

    public AnimeModel Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }
}