﻿using Avalonia;
using Avalonia.Controls;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.ViewModels;
using Ursa.Controls;

namespace TotoroNext.Views;

public partial class MainSplashWindow : SplashWindow
{
    public MainSplashWindow()
    {
        InitializeComponent();
    }
    
    static MainSplashWindow()
    {
        DataContextProperty.Changed.AddClassHandler<MainSplashWindow, object?>((window, e) => OnDataContextChange(e));
    }

    private static void OnDataContextChange(AvaloniaPropertyChangedEventArgs<object?> args)
    {
        if (args.NewValue.Value is SplashViewModel splashViewModel)
        {
            _ = splashViewModel.InitializeAsync();
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