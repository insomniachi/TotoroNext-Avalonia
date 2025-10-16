using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class StoreViewModel : ObservableObject, IAsyncInitializable
{
    private readonly IDialogService _dialogService;
    private readonly SourceCache<ModuleManifest, string> _modulesCache = new(x => x.Id);
    private readonly IModuleStore _moduleStore;
    private readonly ReadOnlyObservableCollection<ModuleManifest> _modules;

    public StoreViewModel(IModuleStore moduleStore,
                          IDialogService dialogService)
    {
        _moduleStore = moduleStore;
        _dialogService = dialogService;

        _modulesCache
            .Connect()
            .RefCount()
            .Filter(this.WhenAnyValue(x => x.SelectedFilterTag).Select(_ => (Func<ModuleManifest, bool>)HasTag))
            .Bind(out _modules)
            .DisposeMany()
            .Subscribe();
    }

    public ReadOnlyObservableCollection<ModuleManifest> Modules => _modules;

    public List<string> FilterTags { get; } =
    [
        "All",
        ComponentTypes.Tracking,
        ComponentTypes.Metadata,
        ComponentTypes.AnimeProvider,
        ComponentTypes.AnimeDownloader,
        ComponentTypes.Debrid,
        ComponentTypes.MediaEngine,
        ComponentTypes.MediaSegments,
        ComponentTypes.Miscellaneous,
    ];

    [ObservableProperty] public partial string SelectedFilterTag { get; set; } = "All";

    public async Task InitializeAsync()
    {
        await foreach (var module in _moduleStore.GetAllModules())
        {
            _modulesCache.AddOrUpdate(module);
        }
    }

    [RelayCommand]
    private async Task DownloadModule(ModuleManifest module)
    {
        await _moduleStore.DownloadModule(module);
        await _dialogService.Information($"{module.Name} downloaded");
    }

    private bool HasTag(ModuleManifest manifest)
    {
        return SelectedFilterTag == "All" || manifest.Categories.Contains(SelectedFilterTag);
    }
}