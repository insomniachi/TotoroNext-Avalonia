using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

public partial class UserListFilter : ObservableObject
{
    private static readonly string[] Properties =
    [
        nameof(Status),
        nameof(Year),
        nameof(Format),
        nameof(ScoreFilter)
    ];

    public UserListFilter()
    {
        var propertyChanged = this.WhenAnyPropertyChanged(Properties).Select(_ => Unit.Default);
        var titleChanged = this.WhenAnyValue(x => x.Term).Throttle(TimeSpan.FromMilliseconds(500)).Select(_ => Unit.Default);
        var genresChanged = Genres.ToObservableChangeSet().Select(_ => Unit.Default);
        Predicate = propertyChanged.Merge(titleChanged)
                                   .Merge(genresChanged)
                                   .Select(_ => (Func<AnimeModel, bool>)IsVisible);
    }

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = ListItemStatus.Watching;

    [ObservableProperty] public partial string Term { get; set; } = "";

    [ObservableProperty] public partial string Year { get; set; } = "";

    [ObservableProperty] public partial AnimeMediaFormat Format { get; set; } = AnimeMediaFormat.Unknown;

    [ObservableProperty] public partial ObservableCollection<string> Genres { get; set; } = [];

    [ObservableProperty] public partial UserScoreFilter ScoreFilter { get; set; }

    public IObservable<Func<AnimeModel, bool>> Predicate { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Status));
    }

    public bool IsVisible(AnimeModel model)
    {
        if (model.Tracking is null)
        {
            return true;
        }

        var listStatusCheck = Status == ListItemStatus.Watching
            ? model.Tracking.Status is ListItemStatus.Watching or ListItemStatus.Rewatching
            : model.Tracking.Status == Status;

        var searchTextStatus = string.IsNullOrEmpty(Term) ||
                               model.Title.Contains(Term, StringComparison.InvariantCultureIgnoreCase) ||
                               model.RomajiTitle.Contains(Term, StringComparison.InvariantCultureIgnoreCase) ||
                               model.EngTitle.Contains(Term, StringComparison.InvariantCultureIgnoreCase);
        var formatCheck = Format == AnimeMediaFormat.Unknown || model.MediaFormat == Format;
        var genresCheck = Genres.All(x => model.Genres.Contains(x));
        var userScoreCheck = ScoreFilter switch
        {
            UserScoreFilter.All => true,
            UserScoreFilter.Scored => model.Tracking.Score > 0,
            UserScoreFilter.Unscored => model.Tracking.Score is null or 0,
            _ => true
        };

        var yearCheck = string.IsNullOrEmpty(Year) || !YearRegex().IsMatch(Year) || model.Season?.Year.ToString() == Year;

        return listStatusCheck && searchTextStatus && yearCheck && formatCheck && genresCheck && userScoreCheck;
    }

    public void Clear()
    {
        Term = "";
        Year = "";
        Format = AnimeMediaFormat.Unknown;
        ScoreFilter = UserScoreFilter.All;
        Genres.Clear();
    }

    [GeneratedRegex(@"(19[5-9][0-9])|(20\d{2})")]
    private partial Regex YearRegex();
}

public enum UserScoreFilter
{
    All,
    Scored,
    Unscored
}