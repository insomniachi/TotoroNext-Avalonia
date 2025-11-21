using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.MediaEngine.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Views;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<EnterFullScreen>(this, (_, _) =>
        {
            WindowState = WindowState.FullScreen;
            UpdateControls(true);
        });
        WeakReferenceMessenger.Default.Register<ExitFullScreen>(this, (_, _) =>
        {
            WindowState = WindowState.Normal;
            UpdateControls(false);
        });

        GetTopLevel(this)!.KeyDown += OnKeyDown;

#if !DEBUG
        DataContextChanged += async (_, e) =>
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
            {
                await vm.CheckForUpdatesAsync();
            }
        };
#endif
    }

    protected override async Task<bool> CanClose()
    {
        var canClose = await base.CanClose();
        if (canClose)
        {
            await App.AppHost.StopAsync();
        }

        return canClose;
    }

    private void UpdateControls(bool isFullscreen)
    {
        MenuContainer.IsVisible = !isFullscreen;
        IsTitleBarVisible = !isFullscreen;
        IsFullScreenButtonVisible = !isFullscreen;
        IsCloseButtonVisible = !isFullscreen;
        CanMinimize = !isFullscreen;
        CanMaximize = !isFullscreen;
        ContentArea.Margin = isFullscreen ? new Thickness(0) : new Thickness(12, 36, 12, 12);
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var gesture = new KeyGesture(e.Key, e.KeyModifiers);
        WeakReferenceMessenger.Default.Send(gesture);
    }
}