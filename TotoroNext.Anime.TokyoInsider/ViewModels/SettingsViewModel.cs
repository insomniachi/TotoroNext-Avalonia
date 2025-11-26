using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace TotoroNext.Anime.TokyoInsider.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [RelayCommand]
    private static async Task UpdateCatalog()
    {
        await Catalog.DownloadCatalog();
    }
}