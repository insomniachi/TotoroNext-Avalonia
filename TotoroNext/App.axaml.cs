using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.Octicons;
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
                      .ConfigureServices((_, services) =>
                      {
                          services.AddCoreServices();
                          services.AddTransient<MainWindowViewModel>();
                          services.AddSingleton<IAnimeOverridesRepository, AnimeOverridesRepository>();
                          services.AddSingleton<SettingsModel>();

                          services.AddInternalMediaPlayer();

                          #if DEBUG
                          services.AddMainNavigationItem<ProviderDebuggerView, ProviderDebuggerViewModel>("Provider Tester",
                                                                                          PackIconOcticonsKind.Beaker16,
                                                                                          new NavMenuItemTag { IsFooterItem = true });
                          #endif
                          
                          services.AddMainNavigationItem<ModulesView, ModulesViewModel>("Installed",
                                                                                        PackIconMaterialDesignKind.ShoppingCart,
                                                                                        new NavMenuItemTag { IsFooterItem = true });
                          services.AddMainNavigationItem<SettingsView, SettingsViewModel>("Settings",
                                                                                          PackIconMaterialDesignKind.Settings,
                                                                                          new NavMenuItemTag { IsFooterItem = true });

                          services.RegisterFactory<ITrackingService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IMediaPlayer>(nameof(SettingsModel.SelectedMediaEngine))
                                  .RegisterFactory<IMetadataService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IAnimeProvider>(nameof(SettingsModel.SelectedAnimeProvider))
                                  .RegisterFactory<IMediaSegmentsProvider>(nameof(SettingsModel.SelectedSegmentsProvider));

                          List<IModule> modules = [new Anime.Module(), ..LoadInstalledModules()];
                          foreach (var module in modules)
                          {
                              module.ConfigureServices(services);
                          }
                      })
                      .Build();

        Container.SetServiceProvider(AppHost.Services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = AppHost.Services.GetService<MainWindowViewModel>()
            };
            desktop.ShutdownRequested += async (_, _) => await AppHost.StopAsync();
        }

        AppHost.StartAsync();

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
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

    private static IEnumerable<IModule> LoadInstalledModules()
    {
#if REFER_PLUGINS
        return new DebugModuleStore().LoadModules();
#else
        return new ModuleStore().LoadModules();
#endif
    }
}


#if REFER_PLUGINS
public class DebugModuleStore : IModuleStore
{
    public IEnumerable<IModule> LoadModules()
    {
        // Anime Providers
        yield return new Anime.AllAnime.Module();
        yield return new Anime.AnimePahe.Module();
        yield return new Anime.AnimeParadise.Module();
        yield return new Anime.AnimeOnsen.Module();

        // Anime Tracking/Metadata
        yield return new Anime.Anilist.Module();
        yield return new Anime.MyAnimeList.Module();

        // Misc
        yield return new Anime.Aniskip.Module();
        yield return new Discord.Module();

        // Media Players
        yield return new MediaEngine.Mpv.Module();
        yield return new MediaEngine.Vlc.Module();
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