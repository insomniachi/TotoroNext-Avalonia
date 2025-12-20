using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.ViewModels;
using Ursa.Controls;

namespace TotoroNext.Views;

public partial class MainSplashWindow : SplashWindow
{
    static MainSplashWindow()
    {
        DataContextProperty.Changed.AddClassHandler<MainSplashWindow, object?>((_, e) => OnDataContextChange(e));
    }

    public MainSplashWindow()
    {
        InitializeComponent();
    }

    private static void OnDataContextChange(AvaloniaPropertyChangedEventArgs<object?> args)
    {
        if (args.NewValue.Value is SplashViewModel splashViewModel)
        {
            splashViewModel.InitializeAsync();
        }
    }

    protected override async Task<Window?> CreateNextWindow()
    {
        await Task.CompletedTask;
        return new MainWindow
        {
            DataContext = App.AppHost.Services.GetService<MainWindowViewModel>()
        };
    }
}