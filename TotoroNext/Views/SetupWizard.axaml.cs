using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;
using TotoroNext.ViewModels;
using Ursa.Controls;

namespace TotoroNext.Views;

public partial class SetupWizard : SplashWindow
{
    public SetupWizard()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SetupWizardViewModel vm)
        {
            return;
        }

        vm.Initialize();
    }

    protected override Task<Window?> CreateNextWindow()
    {
        var service = App.AppHost.Services.GetService<ILocalSettingsService>()!;
        service.SaveSetting("IsSetupComplete", true);
        
        return Task.FromResult<Window?>(new MainWindow
        {
            DataContext = App.AppHost.Services.GetService<MainWindowViewModel>()
        });
    }
}