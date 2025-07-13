using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.ViewModels;

public class AnimeGridViewModel(List<AnimeModel> items) : ObservableObject
{
    public List<AnimeModel> Items { get; } = items;
}