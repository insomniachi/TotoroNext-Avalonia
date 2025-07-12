using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public sealed partial class ModulesViewModel(IEnumerable<Descriptor> modules, IMessenger messenger) : ObservableObject
{
    public List<Descriptor> Descriptors { get; } = [.. modules.Where(x => !x.IsInternal)];

    [RelayCommand]
    private void NavigateToSettings(Descriptor descriptor)
    {
        if (descriptor.SettingViewModel is not { } vmType)
        {
            return;
        }

        messenger.Send(new PaneNavigateToViewModelMessage(vmType, paneWidth: 600, title: descriptor.Name));
    }
}