using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class UserListSortAndFilterViewModel(
    UserListSortAndFilter sortAndFilter,
    IFactory<IMetadataService, Guid> metadataServiceFactory) : DialogViewModel, IAsyncInitializable
{
    public UserListFilter Filter { get; } = sortAndFilter.Filter;
    public UserListSort Sort { get; } = sortAndFilter.Sort;
    [ObservableProperty] public partial List<string> Genres { get; set; } = [];

    public async Task InitializeAsync()
    {
        var service = metadataServiceFactory.CreateDefault();
        Genres = await service.GetGenresAsync();
    }
}