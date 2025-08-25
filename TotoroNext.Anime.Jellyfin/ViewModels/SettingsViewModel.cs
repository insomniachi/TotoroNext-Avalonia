using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Jellyfin.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    private readonly JellyfinSdkSettings _jellyfinSettings;
    private readonly JellyfinApiClient _client;

    public SettingsViewModel(IModuleSettings<Settings> settings,
                             JellyfinSdkSettings jellyfinSettings,
                             JellyfinApiClient client) : base(settings)
    {
        _jellyfinSettings = jellyfinSettings;
        _client = client;
        
        Username = settings.Value.Username;
        Password = settings.Value.Password;
        ServerUrl = settings.Value.ServerUrl;
    }

    public string? Username
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Username = value);
    }
    
    public string? Password
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Password = value);
    }

    public string? ServerUrl
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.ServerUrl = value);
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrEmpty(ServerUrl) ||
            string.IsNullOrEmpty(Username) ||
            string.IsNullOrEmpty(Password))
        {
            return;
        }
        
        var id = Environment.MachineName;
        _jellyfinSettings.SetServerUrl(ServerUrl);
        _jellyfinSettings.Initialize("Totoro", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), Environment.MachineName, id);
        
        await _client.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
        {
            Username = Username,
            Pw = Password
        });
        
    }
}