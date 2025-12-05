using MalApi;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList;

public class Initializer(IModuleSettings<Settings> settings) : IBackgroundInitializer
{
    public async Task BackgroundInitializeAsync()
    {
        if (settings.Value.Auth is not { } auth)
        {
            return;
        }

        if (!auth.IsExpired)
        {
            return;
        }

        settings.Value.Auth = await MalAuthHelper.RefreshToken(Settings.ClientId, auth.RefreshToken);
    }
}