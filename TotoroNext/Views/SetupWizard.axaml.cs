using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using TotoroNext.ViewModels;

namespace TotoroNext.Views;

public partial class SetupWizard : Window
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
}