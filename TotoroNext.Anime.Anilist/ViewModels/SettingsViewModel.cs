using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TotoroNext.Anime.Anilist.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Anilist.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
        TitleLanguage = settings.Value.TitleLanguage;
        Token = settings.Value.Token;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public string? Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Token = value);
    }

    public double SearchLimit
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SearchLimit = value);
    }

    public TitleLanguage TitleLanguage
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.TitleLanguage = value);
    }

    public TitleLanguage[] TitleLanguages { get; } = [TitleLanguage.English, TitleLanguage.Romaji];

    public async Task Login(ILauncher launcher, IToastManager toastManager)
    {
        await launcher.LaunchUriAsync(new Uri($"https://anilist.co/api/v2/oauth/authorize?client_id={Settings.ClientId}&response_type=token"));

        var options = new DialogOptions
        {
            Title = "Copy & Paste the text from the browser",
            Mode = DialogMode.Info,
            Button = DialogButton.OKCancel,
            CanDragMove = false,
            IsCloseButtonVisible = false,
            CanResize = false,
            ShowInTaskBar = false,
            StartupLocation = WindowStartupLocation.CenterOwner
        };

        var vm = new GetAnilistTokenDialogViewModel();
        var result = await Dialog.ShowModal<GetAnilistTokenDialog, GetAnilistTokenDialogViewModel>(vm, options: options);

        if (result == DialogResult.OK)
        {
            Token = vm.Token;

            toastManager.Show(new Toast
            {
                Content = "Anilist Authenticated",
                Expiration = TimeSpan.FromSeconds(2),
                Type = Avalonia.Controls.Notifications.NotificationType.Success
            });
        }
    }
}