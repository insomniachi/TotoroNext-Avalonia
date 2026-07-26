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

    protected override Task<Window?> CreateNextWindow()
    {
        // return Task.FromResult<Window?>(new SetupWizard()
        // {
        //     DataContext = App.AppHost.Services.GetService<SetupWizardViewModel>()
        // });

        return Task.FromResult<Window?>(new MainWindow
        {
            DataContext = App.AppHost.Services.GetService<MainWindowViewModel>()
        });
    }
}