using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class UserListSort : ObservableObject
{
    public UserListSort()
    {
        Comparer = this.WhenAnyValue(x => x.IsAscending, x => x.Field)
            .Select(x => GetSortComparer(x.Item1, x.Item2))
            .WhereNotNull();
    }

    [ObservableProperty] public partial bool IsAscending { get; set; } = true;
    [ObservableProperty] public partial SortField Field { get; set; } = SortField.Title;
    public IObservable<SortExpressionComparer<AnimeModel>> Comparer { get; }

    private static SortExpressionComparer<AnimeModel>? GetSortComparer(bool isAscending, SortField field)
    {
        return field switch
        {
            SortField.Title => CreateComparer(x => x.Title, isAscending),
            SortField.MeanScore => CreateComparer(x => x.MeanScore ?? 0, isAscending),
            SortField.UserScore => CreateComparer(x => x.Tracking?.Score ?? 0, isAscending),
            SortField.DateCompleted => CreateComparer(x => x.Tracking?.FinishDate ?? DateTime.MinValue, isAscending),
            _ => null
        };
    }

    private static SortExpressionComparer<AnimeModel> CreateComparer(Func<AnimeModel, IComparable> expression, bool isAscending)
    {
        return [new SortExpression<AnimeModel>(expression, isAscending ? SortDirection.Ascending : SortDirection.Descending)];
    }
}

public enum SortField
{
    Title,
    MeanScore,
    UserScore,
    DateCompleted
}