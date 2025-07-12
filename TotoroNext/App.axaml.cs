using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.ViewModels;
using TotoroNext.Views;

namespace TotoroNext;

public class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppHost = Host.CreateDefaultBuilder()
                      .ConfigureServices(async (_, services) =>
                      {
                          services.AddCoreServices();
                          services.AddTransient<MainWindowViewModel>();
                          services.AddSingleton<IAnimeOverridesRepository, AnimeOverridesRepository>();

                          services.RegisterFactory<ITrackingService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IMediaPlayer>(nameof(SettingsModel.SelectedMediaEngine))
                                  .RegisterFactory<IMetadataService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IAnimeProvider>(nameof(SettingsModel.SelectedAnimeProvider))
                                  .RegisterFactory<IMediaSegmentsProvider>(nameof(SettingsModel.SelectedSegmentsProvider));

                          var modules = new List<IModule>
                          {
                              new Anime.Module(),
                              new Anime.Anilist.Module(),
                              new Anime.AllAnime.Module(),
                              new MediaEngine.Mpv.Module(),
                          };

                          foreach (var module in modules)
                          {
                              module.ConfigureServices(services);
                          }
                      })
                      .Build();

        Container.SetServiceProvider(AppHost.Services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = AppHost.Services.GetService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}


#if DEBUG
public class DebugModuleStore : IModuleStore
{
    public async IAsyncEnumerable<IModule> LoadModules()
    {
        await Task.CompletedTask;

        // Anime Providers
        yield return new Anime.AllAnime.Module();
        //yield return new Anime.AnimePahe.Module();

        // Anime Tracking/Metadata
        yield return new Anime.Anilist.Module();
        //yield return new Anime.MyAnimeList.Module();

        // Misc
        //yield return new Anime.Aniskip.Module();
        //yield return new Discord.Module();

        // Media Players
        yield return new MediaEngine.Mpv.Module();
        //yield return new MediaEngine.Vlc.Module();
    }

    public Task<bool> DownloadModule(ModuleManifest manifest)
    {
        return Task.FromResult(false);
    }

    public IAsyncEnumerable<ModuleManifest> GetAllModules()
    {
        return AsyncEnumerable.Empty<ModuleManifest>();
    }
}
#endif