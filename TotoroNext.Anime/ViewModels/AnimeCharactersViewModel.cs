using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeCharactersViewModel(
    CharactersViewNavigationParameters parameters,
    IFactory<IMetadataService, Guid> metadataServiceFactory) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial List<CharacterModel> Characters { get; set; } = [];

    public async Task InitializeAsync()
    {
        var service = metadataServiceFactory.CreateFor(parameters.Anime);
        if (service is null)
        {
            return;
        }

        Characters = await service.GetCharactersAsync(parameters.Anime.Id);
    }
}