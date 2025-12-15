using System.Reflection;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using JetBrains.Annotations;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Jellyfin;

[UsedImplicitly]
public class Authenticator(
    JellyfinSdkSettings jellyfinSettings,
    JellyfinApiClient client,
    IModuleSettings<Settings> settings)
{
    public async ValueTask<bool> LoginIfNotAuthenticated()
    {
        if (string.IsNullOrEmpty(settings.Value.ServerUrl) ||
            string.IsNullOrEmpty(settings.Value.Username) ||
            string.IsNullOrEmpty(settings.Value.Password))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(jellyfinSettings.AccessToken))
        {
            return true;
        }

        var id = Environment.MachineName;
        jellyfinSettings.SetServerUrl(settings.Value.ServerUrl);
        jellyfinSettings.Initialize("Totoro", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), Environment.MachineName, id);

        try
        {
            var result = await client.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
            {
                Username = settings.Value.Username,
                Pw = settings.Value.Password
            });

            if (result is null)
            {
                return false;
            }

            Settings.UserId = result.User?.Id;
            Settings.AccessToken = result.AccessToken;

            jellyfinSettings.SetAccessToken(result.AccessToken);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}