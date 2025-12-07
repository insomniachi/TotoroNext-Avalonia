using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public sealed partial class ModulesViewModel : ObservableObject
{
    private readonly ReadOnlyObservableCollection<Descriptor> _descriptors;
    private readonly SourceCache<Descriptor, Guid> _descriptorsCache = new(x => x.Id);
    private readonly IMessenger _messenger;

    public ModulesViewModel(IEnumerable<Descriptor> modules,
                            IMessenger messenger)
    {
        _messenger = messenger;

        _descriptorsCache
            .Connect()
            .RefCount()
            .Filter(this.WhenAnyValue(x => x.SelectedFilterTag).Select(_ => (Func<Descriptor, bool>)HasTag))
            .Bind(out _descriptors)
            .DisposeMany()
            .Subscribe();

        _descriptorsCache.AddOrUpdate([.. modules.Where(x => !x.IsInternal)]);
    }

    public ReadOnlyObservableCollection<Descriptor> Descriptors => _descriptors;

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
        ComponentTypes.Miscellaneous
    ];

    [ObservableProperty] public partial string SelectedFilterTag { get; set; } = "All";

    [RelayCommand]
    private void NavigateToSettings(Descriptor descriptor)
    {
        if (descriptor.SettingViewModel is not { } vmType)
        {
            return;
        }

        _messenger.Send(new PaneNavigateToViewModelMessage(vmType, paneWidth: 600, title: descriptor.Name));
    }

    private bool HasTag(Descriptor descriptor)
    {
        return SelectedFilterTag == "All" || descriptor.Components.Contains(SelectedFilterTag);
    }
}