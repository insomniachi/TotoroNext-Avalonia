using Avalonia;
using Avalonia.Controls;

namespace TotoroNext.Anime.Abstractions.Controls;

public partial class AnimeCard : UserControl
{
    public static readonly StyledProperty<AnimeModel> AnimeProperty =
        AvaloniaProperty.Register<AnimeCard, AnimeModel>(nameof(Anime));

    public AnimeCard()
    {
        InitializeComponent();
    }

    public AnimeModel Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }
}