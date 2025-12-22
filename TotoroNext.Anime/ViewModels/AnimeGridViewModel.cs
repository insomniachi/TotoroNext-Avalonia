using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeGridViewModel(
    List<AnimeModel> items,
    IMessenger messenger) : ObservableObject
{
    public List<AnimeModel> Items { get; } = items;


    [RelayCommand]
    private void OpenAnimeDetails(AnimeModel anime)
    {
        messenger.Send(new PaneNavigateToDataMessage(anime, paneWidth: 750, title: anime.Title));
    }
}