using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class StoreViewModel(
    IModuleStore moduleStore,
    IDialogService dialogService) : ObservableObject, IAsyncInitializable
{
    public ObservableCollection<ModuleManifest> Modules { get; } = [];

    public async Task InitializeAsync()
    {
        await foreach (var module in moduleStore.GetAllModules())
        {
            Modules.Add(module);
        }
    }

    [RelayCommand]
    private async Task DownloadModule(ModuleManifest module)
    {
        await moduleStore.DownloadModule(module);
        await dialogService.Information($"{module.Name} downloaded");
    }
}