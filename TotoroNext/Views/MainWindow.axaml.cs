using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Views;

public partial class MainWindow : UrsaWindow
{
    private readonly IKeyBindingsManager _keyBindingsManager = Container.Services.GetRequiredService<IKeyBindingsManager>();
    
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
        
        KeyDown += OnKeyDown;
    }
    
    private void UpdateControls(bool isFullscreen)
    {
        MenuContainer.IsVisible = !isFullscreen;
        IsTitleBarVisible = !isFullscreen;
        ThemeToggleButton.IsVisible = !isFullscreen;
        IsFullScreenButtonVisible = !isFullscreen;
        IsCloseButtonVisible = !isFullscreen;
        IsMinimizeButtonVisible = !isFullscreen;
        IsRestoreButtonVisible = !isFullscreen;
        ContentArea.Margin = isFullscreen ? new Thickness(0) : new Thickness(12,36,12,12);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var gesture = new KeyGesture(e.Key, e.KeyModifiers);
        var keyBinding = _keyBindingsManager.KeyBindings.FirstOrDefault(x => gesture.Equals(x.Gesture));
        keyBinding?.Command.Execute(keyBinding.CommandParameter);
    }
}