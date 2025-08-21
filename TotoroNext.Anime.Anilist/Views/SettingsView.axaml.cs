﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using TotoroNext.Anime.Anilist.ViewModels;
using Ursa.Controls;

namespace TotoroNext.Anime.Anilist.Views;

public partial class SettingsView : UserControl
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

            var launcher = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                                                    ? desktop.MainWindow
                                                    : null)?.Launcher;

            if (launcher is null)
            {
                return;
            }

            await vm.Login(launcher, new WindowToastManager(TopLevel.GetTopLevel(this)) { MaxItems = 3 });
        }
        catch
        {
            Console.WriteLine("Failed");
        }
    }
}