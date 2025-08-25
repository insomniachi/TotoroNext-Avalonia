using System.Reflection;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Jellyfin;

public class Initializer(JellyfinSdkSettings jellyfinSettings,
                         JellyfinApiClient client,
                         IModuleSettings<Settings> settings) : IBackgroundInitializer
{
    public async Task BackgroundInitializeAsync()
    {
        if (string.IsNullOrEmpty(settings.Value.ServerUrl) ||
            string.IsNullOrEmpty(settings.Value.Username) ||
            string.IsNullOrEmpty(settings.Value.Password))
        {
            return;
        }
        
        var id = Environment.MachineName;
        jellyfinSettings.SetServerUrl(settings.Value.ServerUrl);
        jellyfinSettings.Initialize("Totoro", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), Environment.MachineName, id);
        
        var result = await client.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
        {
            Username = settings.Value.Username,
            Pw = settings.Value.Password
        });

        if (result is null)
        {
            return;
        }

        Settings.UserId = result.User?.Id;
        Settings.AccessToken = result.AccessToken;
        
        jellyfinSettings.SetAccessToken(result.AccessToken);
    }
}