using System.Collections.Specialized;
using Avalonia.Platform.Storage;
using MalApi;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList.ViewModels;

public sealed class SettingsViewModel : ModuleSettingsViewModel<Settings>, IDisposable
{
    private readonly OAuthListener _listener;

    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        _listener = new OAuthListener(2222, ProcessQuery);

        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public OAuthToken? Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Auth = value);
    }

    public int SearchLimit
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SearchLimit = value);
    }

    public void Dispose()
    {
        _listener.Stop();
    }

    public async Task Login(ILauncher launcher)
    {
        _listener.Start();
        await launcher.LaunchUriAsync(new Uri(MalAuthHelper.GetAuthUrl(Settings.ClientId)));
    }

    private async Task ProcessQuery(NameValueCollection query)
    {
        var code = query["code"];
        Token = await MalAuthHelper.DoAuth(Settings.ClientId, code);
    }
}