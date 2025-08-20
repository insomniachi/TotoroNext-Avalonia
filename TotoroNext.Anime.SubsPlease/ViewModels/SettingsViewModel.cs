using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TotoroNext.Anime.SubsPlease.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [RelayCommand]
    private static async Task UpdateCatalog()
    {
        await Catalog.DownloadCatalog();
    }
}