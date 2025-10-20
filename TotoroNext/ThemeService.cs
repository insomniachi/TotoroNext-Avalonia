﻿using Avalonia;
using Avalonia.Styling;
using Microsoft.Extensions.Hosting;
using TotoroNext.ViewModels;
using Ursa.Themes.Semi;

namespace TotoroNext;

public class ThemeService(SettingsModel settings) : IHostedService
{
    private static readonly Dictionary<string, ThemeVariant> Themes = new()
    {
        {"Default", ThemeVariant.Default},
        {"Light", ThemeVariant.Light},
        {"Dark", ThemeVariant.Dark},
        {"Aquatic", SemiTheme.Aquatic},
        {"Desert", SemiTheme.Desert},
        {"Dusk", SemiTheme.Dusk},
        {"NightSky", SemiTheme.NightSky}
    };

    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplyTheme(settings.SelectedTheme);
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsModel.SelectedTheme))
            {
                ApplyTheme(settings.SelectedTheme);
            }
        };

        if (string.IsNullOrEmpty(settings.SelectedTheme))
        {
            settings.SelectedTheme = "Dark";
        }
        
        return Task.CompletedTask;
    }

    private static void ApplyTheme(string settingsSelectedTheme)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        if (string.IsNullOrEmpty(settingsSelectedTheme))
        {
            return;
        }
        
        application.RequestedThemeVariant = Themes[settingsSelectedTheme];
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}