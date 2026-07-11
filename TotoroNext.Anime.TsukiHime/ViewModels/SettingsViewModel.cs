using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl.Http;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SettingsViewModel(IModuleSettings<Settings> data,
                             IHttpClientFactory httpClientFactory) : base(data)
    {
        _httpClientFactory = httpClientFactory;
        SelectedGroup = TsukiHimeGroups.Groups.FirstOrDefault(x => x.Id == data.Value.Group);
    }

    public GroupDescriptor? SelectedGroup
    {
        get;
        set => SetAndSaveProperty(ref field, value, v => v.Group = SelectedGroup?.Id ?? 10);
    }

    [RelayCommand]
    private async Task FetchGroups()
    {
        using var client = new FlurlClient(_httpClientFactory.CreateClient($"{Module.Id}-api"));
        await TsukiHimeGroups.FetchAsync(client);
    }
}