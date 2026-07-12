using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl.Http;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>, IInitializable
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SettingsViewModel(IModuleSettings<Settings> data,
                             IHttpClientFactory httpClientFactory) : base(data)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    [RelayCommand]
    private async Task FetchGroups()
    {
        using var client = new FlurlClient(_httpClientFactory.CreateClient($"{Module.Id}-api"));
        await TsukiHimeLocalData.FetchGroupsAsync(client);
    }

    [RelayCommand]
    private async Task FetchAnime()
    {
        using var client = new FlurlClient(_httpClientFactory.CreateClient($"{Module.Id}-api"));
        await TsukiHimeLocalData.FetchAnimeAsync(client);
    }
}