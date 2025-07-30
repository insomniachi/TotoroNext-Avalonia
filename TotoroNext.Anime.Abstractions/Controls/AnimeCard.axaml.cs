using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace TotoroNext.Anime.Abstractions.Controls;

public partial class AnimeCard : UserControl
{
    public static readonly StyledProperty<AnimeModel> AnimeProperty =
        AvaloniaProperty.Register<AnimeCard, AnimeModel>(nameof(Anime));

    private static readonly SolidColorBrush AiringBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush FinishedBrush = new(Colors.MediumSlateBlue);
    private static readonly SolidColorBrush NotYetBrush = new(Colors.LightSlateGray);
    private static readonly SolidColorBrush OtherBrush = new(Colors.Transparent);

    public AnimeCard()
    {
        InitializeComponent();

        AnimeProperty.Changed.AddClassHandler<AnimeCard>(OnAnimeChanged);
    }

    public AnimeModel Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }

    private static void OnAnimeChanged(AnimeCard card, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not AnimeModel anime)
        {
            return;
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            UpdateBadge(card, anime);
            UpdateAiringTime(card, anime);
            card.StatusBorder.BorderBrush = ToBrush(anime);
        });
    }

    private static void UpdateBadge(AnimeCard card, AnimeModel anime)
    {
        var count = ToUnWatchedEpisodes(anime);
        if (count <= 0)
        {
            return;
        }

        card.Badge.IsVisible = true;
        card.BadgeText.Text = count.ToString();
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

    private static int ToUnWatchedEpisodes(AnimeModel? anime)
    {
        if (anime is null)
        {
            return 0;
        }
        
        return anime.AiredEpisodes - (anime.Tracking?.WatchedEpisodes ?? 0);
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

    private static SolidColorBrush ToBrush(AnimeModel anime)
    {
        return anime?.AiringStatus switch
        {
            AiringStatus.CurrentlyAiring => AiringBrush,
            AiringStatus.FinishedAiring => FinishedBrush,
            AiringStatus.NotYetAired => NotYetBrush,
            _ => OtherBrush
        };
    }
}