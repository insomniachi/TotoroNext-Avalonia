using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using TotoroNext.Anime.MyAnimeList.ViewModels;

namespace TotoroNext.Anime.MyAnimeList.Views;

public partial class SettingsView : ContentPage
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is not SettingsViewModel vm)
            {
                return;
            }
            
            await vm.Login();
        }
        catch
        {
            Console.WriteLine("Failed");
        }
    }
}