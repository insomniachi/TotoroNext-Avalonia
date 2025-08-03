using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

public partial class UserListFilter : ObservableObject
{
    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = ListItemStatus.Watching;

    [ObservableProperty] public partial string Term { get; set; } = "";

    [ObservableProperty] public partial string Year { get; set; } = "";

    [ObservableProperty] public partial AnimeMediaFormat Format { get; set; } = AnimeMediaFormat.Unknown;
    
    [ObservableProperty] public partial ObservableCollection<string> Genres { get; set; } = [];
    
    public IObservable<Func<AnimeModel, bool>> Predicate { get; }

    public UserListFilter()
    {
        var propertyChanged = this.WhenAnyPropertyChanged().Select(_ => Unit.Default);
        var genresChanged = Genres.ToObservableChangeSet().Select(_ => Unit.Default);
        Predicate = propertyChanged.Merge(genresChanged).Select(_ => (Func<AnimeModel, bool>)IsVisible);
    }

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
                               model.Title.Contains(Term, StringComparison.InvariantCultureIgnoreCase);

        var formatCheck = Format == AnimeMediaFormat.Unknown || model.MediaFormat == Format;
        var genresCheck = Genres.All(x => model.Genres.Contains(x));

        //var searchTextStatus = string.IsNullOrEmpty(Term) ||
        //                       model.Title.Contains(Term, StringComparison.InvariantCultureIgnoreCase) ||
        //                       model.AlternativeTitles.Any(x => x.Contains(Term, StringComparison.InvariantCultureIgnoreCase));
        var yearCheck = string.IsNullOrEmpty(Year) || !YearRegex().IsMatch(Year) || model.Season?.Year.ToString() == Year;
        //var genresCheck = !Genres.Any() || Genres.All(x => model.Genres.Any(y => string.Equals(y, x, StringComparison.InvariantCultureIgnoreCase)));
        //var airingStatusCheck = AiringStatus is null || AiringStatus == model.AiringStatus;

        var isVisible = listStatusCheck && searchTextStatus && yearCheck && formatCheck && genresCheck /* && genresCheck && airingStatusCheck*/;

        return isVisible;
    }

    public void Clear()
    {
        Term = "";
        Year = "";
        Format = AnimeMediaFormat.Unknown;
        Genres.Clear();
    }

    [GeneratedRegex(@"(19[5-9][0-9])|(20\d{2})")]
    private partial Regex YearRegex();
}