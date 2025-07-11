using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

public partial class UserListFilter : ObservableObject
{
    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = ListItemStatus.Watching;

    [ObservableProperty] public partial string Term { get; set; } = "";

    [ObservableProperty] public partial string Year { get; set; } = "";

    public void Refresh() => OnPropertyChanged(nameof(Status));
    
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

        //var searchTextStatus = string.IsNullOrEmpty(Term) ||
        //                       model.Title.Contains(Term, StringComparison.InvariantCultureIgnoreCase) ||
        //                       model.AlternativeTitles.Any(x => x.Contains(Term, StringComparison.InvariantCultureIgnoreCase));
        var yearCheck = string.IsNullOrEmpty(Year) || !YearRegex().IsMatch(Year) || model.Season?.Year.ToString() == Year;
        //var genresCheck = !Genres.Any() || Genres.All(x => model.Genres.Any(y => string.Equals(y, x, StringComparison.InvariantCultureIgnoreCase)));
        //var airingStatusCheck = AiringStatus is null || AiringStatus == model.AiringStatus;

        var isVisible = listStatusCheck && searchTextStatus && yearCheck /* && genresCheck && airingStatusCheck*/;

        return isVisible;
    }

    public void Clear()
    {
        Term = "";
        Year = "";
    }

    [GeneratedRegex(@"(19[5-9][0-9])|(20\d{2})")]
    private partial Regex YearRegex();
}

