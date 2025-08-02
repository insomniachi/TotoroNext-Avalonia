using JetBrains.Annotations;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed class UserListSortAndFilterViewModel(UserListSortAndFilter sortAndFilter)
{
    public UserListFilter Filter { get; } = sortAndFilter.Filter;
    public UserListSort Sort { get; } = sortAndFilter.Sort;
}
