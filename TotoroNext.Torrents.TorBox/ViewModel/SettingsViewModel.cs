using CommunityToolkit.Mvvm.Input;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Torrents.TorBox.ViewModel;

internal partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    private readonly TorBoxService _torboxService;

    public SettingsViewModel(IModuleSettings<Settings> settings,
                             TorBoxService torboxService) : base(settings)
    {
        _torboxService = torboxService;
        Token = settings.Value.Token;
    }

    public string Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Token = value);
    }

    [RelayCommand]
    private async Task DeleteAll()
    {
        await _torboxService.DeleteAllTorrents();
    }
}