using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using IconPacks.Avalonia.Lucide;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.Octicons;
using Irihi.Avalonia.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using TotoroNext.Views;
using Ursa.Controls;

namespace TotoroNext.ViewModels;

public partial class SplashViewModel(IHostBuilder hostBuilder) : ObservableObject, IDialogContext
{
    [ObservableProperty] public partial string PrimaryText { get; set; } = "";
    [ObservableProperty] public partial string SecondaryText { get; set; } = "";

    public void Close()
    {
        UpdateStatus("Launching application...", "");
    }

    public event EventHandler<object?>? RequestClose;

    public async Task InitializeAsync()
    {
        WeakReferenceMessenger.Default.Register<Tuple<string, string>>(this, (_, message) => { UpdateStatus(message.Item1, message.Item2); });

        await BuildServiceProvider();
        await StartBackgroundServicesAsync();
        RequestClose?.Invoke(this, DialogResult.OK);
    }

    private async Task BuildServiceProvider()
    {
        var store = CreateStore();

        UpdateStatus("Updating modules...", "");
        await UpdateModules(store);

        UpdateStatus("Building service provider...", "");

        App.AppHost = hostBuilder
                      .ConfigureServices((_, services) =>
                      {
                          services.AddCoreServices();
                          services.AddTransient<MainWindowViewModel>();
                          services.AddSingleton<IAnimeExtensionService, AnimeExtensionService>();
                          services.AddSingleton<SettingsModel>();

#if REFER_PLUGINS
                          services.AddSingleton<IModuleStore, DebugModuleStore>();
#else
                          services.AddSingleton<IModuleStore, ModuleStore>();
#endif

                          services.AddInternalMediaPlayer();

                          services.RegisterFactory<ITrackingService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IMediaPlayer>(nameof(SettingsModel.SelectedMediaEngine))
                                  .RegisterFactory<IMetadataService>(nameof(SettingsModel.SelectedTrackingService))
                                  .RegisterFactory<IAnimeProvider>(nameof(SettingsModel.SelectedAnimeProvider))
                                  .RegisterFactory<IMediaSegmentsProvider>(nameof(SettingsModel.SelectedSegmentsProvider))
                                  .RegisterFactory<IDebrid>(nameof(SettingsModel.SelectedDebridService));

                          RegisterNavigationViewItems(services);

                          UpdateStatus("Loading modules...", "");
                          List<IModule> modules =
                          [
                              new Anime.Module(),
                              new SongRecognition.Module(),
                              ..store.LoadModules()
                          ];

                          UpdateStatus("Initializing modules...", "");
                          foreach (var module in modules)
                          {
                              module.ConfigureServices(services);
                          }
                      })
                      .Build();
        Container.SetServiceProvider(App.AppHost.Services);
    }

    private static async Task UpdateModules(IModuleStore store)
    {
        await foreach (var manifest in store.GetAllModules())
        {
            var folder = GetModuleFolder(manifest.EntryPoint.Replace(".dll", ""));
            var exists = Directory.Exists(folder);
            var needDownload = !exists;

            if (exists)
            {
                var latestVersionInfo = manifest.Versions[0].Version.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var latestVersion = new Version(int.Parse(latestVersionInfo[0]),
                                                int.Parse(latestVersionInfo[1]),
                                                int.Parse(latestVersionInfo[2]));
                var entryPoint = Path.Combine(folder, manifest.EntryPoint);
                if (File.Exists(entryPoint))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(entryPoint);
                    var currentVersion = new Version(versionInfo.FileMajorPart,
                                                     versionInfo.FileMinorPart,
                                                     versionInfo.FileBuildPart,
                                                     versionInfo.FilePrivatePart);

                    needDownload = currentVersion < latestVersion;
                }
            }

            if (!needDownload)
            {
                continue;
            }

            await store.DownloadModule(manifest);
        }
    }

    private async Task StartBackgroundServicesAsync()
    {
        UpdateStatus("Initializing services...", "");

        if (App.AppHost.Services.GetService<IEnumerable<IInitializer>>() is { } initializers)
        {
            foreach (var initializer in initializers)
            {
                UpdateStatus(null, initializer.GetType().Name);
                initializer.Initialize();
            }
        }

        UpdateStatus("Starting background services...", "");

        await App.AppHost.StartAsync();
    }

    private static IModuleStore CreateStore()
    {
#if REFER_PLUGINS
        return new DebugModuleStore();
#else
        return new ModuleStore();
#endif
    }

    private static void RegisterNavigationViewItems(IServiceCollection services)
    {
#if DEBUG
        services.AddMainNavigationItem<ProviderDebuggerView, ProviderDebuggerViewModel>("Provider Tester",
                                                                                        PackIconOcticonsKind.Beaker16,
                                                                                        new NavMenuItemTag { IsFooterItem = true });

        services.AddParentNavigationViewItem("AniGuesser", PackIconMaterialDesignKind.QuestionMark,
                                             new NavMenuItemTag { Order = 3 });
#endif

        services.AddMainNavigationItem<StoreView, StoreViewModel>("Store",
                                                                  PackIconLucideKind.Store,
                                                                  new NavMenuItemTag
                                                                  {
                                                                      IsFooterItem = true
                                                                  });

        services.AddMainNavigationItem<ModulesView, ModulesViewModel>("Installed",
                                                                      PackIconMaterialDesignKind.ShoppingCart,
                                                                      new NavMenuItemTag
                                                                      {
                                                                          IsFooterItem = true
                                                                      });
        services.AddMainNavigationItem<SettingsView, SettingsViewModel>("Settings",
                                                                        PackIconMaterialDesignKind.Settings,
                                                                        new NavMenuItemTag
                                                                        {
                                                                            IsFooterItem = true,
                                                                            Order = int.MaxValue
                                                                        });
    }

    private void UpdateStatus(string? primary, string? secondary)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (primary is not null)
            {
                PrimaryText = primary;
            }

            if (secondary is not null)
            {
                SecondaryText = secondary;
            }
        });
    }

    private static string GetModuleFolder(string name)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "TotoroNext",
                            "Modules",
                            name);
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
        yield return new Anime.Anizone.Module();
        yield return new Anime.SubsPlease.Module();
        yield return new Anime.Jellyfin.Module();

        // Anime Tracking/Metadata
        yield return new Anime.Anilist.Module();
        yield return new Anime.MyAnimeList.Module();

        // Misc
        yield return new Anime.Aniskip.Module();
        yield return new Discord.Module();

        // Media Players
        yield return new MediaEngine.Mpv.Module();
        yield return new MediaEngine.Vlc.Module();
        
        // Debrid
        yield return new Torrents.TorBox.Module();
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