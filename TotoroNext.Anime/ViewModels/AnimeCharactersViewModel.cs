using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeCharactersViewModel(
    CharactersViewNavigationParameters parameters,
    IFactory<IMetadataService, Guid> metadataServiceFactory) : ObservableObject, IAsyncInitializable
{
    private readonly IMetadataService? _metadataService = metadataServiceFactory.CreateDefault();

    [ObservableProperty] public partial List<CharacterModel> Characters { get; set; } = [];

    public async Task InitializeAsync()
    {
        if (_metadataService is null)
        {
            return;
        }
        
        Characters = await _metadataService.GetCharactersAsync(parameters.Anime.Id);
    }
}