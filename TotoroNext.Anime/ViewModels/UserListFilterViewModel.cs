namespace TotoroNext.Anime.ViewModels;

public sealed class UserListFilterViewModel(UserListFilter filter)
{
    public UserListFilter Filter { get; } = filter;
}