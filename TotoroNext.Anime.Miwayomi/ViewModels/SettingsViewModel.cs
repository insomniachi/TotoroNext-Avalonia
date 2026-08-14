using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flurl;
using Flurl.Http;
using ReactiveUI.Reactive;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miwayomi.ViewModels;

internal partial class SettingsViewModel(IModuleSettings<Settings> settings,
                                         IDialogService dialogService) : ModuleSettingsViewModel<Settings>(settings)
{
    private List<MiwayomiProviderViewModel> _allProviders = [];

    [ObservableProperty] public partial List<MiwayomiProvider> Providers { get; private set; } = [];

    [ObservableProperty] public partial List<MiwayomiProviderViewModel> FilteredProviders { get; set; } = [];

    [ObservableProperty] public partial MiwayomiProvider? SelectedProvider { get; set; }

    [ObservableProperty] public partial string SearchText { get; set; } = "";

    [ObservableProperty] public partial List<string> Languages { get; set; } = [];

    [ObservableProperty] public partial string SelectedLanguage { get; set; } = "all";

    [ObservableProperty] public partial ObservableCollection<RepositoryDescriptor> Repositories { get; set; } = [];

    public string? BaseUrl
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.BaseUrl = value ?? "");
    }

    public RepositoryDescriptor? SelectedRepository
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Repository = value?.Url);
    }

    public override void Initialize()
    {
        BaseUrl = Settings.BaseUrl;
        Repositories = [.. Settings.Repositories];
        SelectedRepository = Repositories.FirstOrDefault(x => x.Url == Settings.Repository);

        this.WhenAnyValue(x => x.BaseUrl)
            .Throttle(TimeSpan.FromSeconds(1))
            .Select(UpdateProviders)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedProvider)
            .WhereNotNull()
            .Subscribe(p =>
            {
                Settings.SelectedSource = p.Id;
                Save();
            });

        this.WhenAnyValue(x => x.SearchText, x => x.SelectedLanguage)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(x =>
            {
                var (text, lang) = x;
                text = text.Replace(" ", "");

                IEnumerable<MiwayomiProviderViewModel> filtered = _allProviders;

                if (lang != "all")
                {
                    filtered = _allProviders.Where(p => p.Sources.Any(s => s.Language == lang));
                }

                if (text.Length > 2)
                {
                    filtered = filtered.Where(p => p.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase));
                }

                FilteredProviders = [.. filtered];
            });
    }

    private async Task UpdateProviders(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            var stream = await $"{url}/api/v1/sources".GetStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            Providers = doc.RootElement.GetProperty("anime").Deserialize<List<MiwayomiProvider>>() ?? [];
            SelectedProvider = Providers.FirstOrDefault(x => x.Id == Settings.SelectedSource);

            stream = await $"{url}/api/v1/extensions/repo".AppendQueryParam("url", Settings.Repository).GetStreamAsync();
            doc = await JsonDocument.ParseAsync(stream);
            FilteredProviders = _allProviders = doc.RootElement.GetProperty("extensions").Deserialize<List<MiwayomiProviderViewModel>>() ?? [];
            Languages = [.. _allProviders.SelectMany(x => x.Sources).Select(x => x.Language).Distinct()];
            SelectedLanguage = Languages.First();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async Task InstallExtension(MiwayomiProviderViewModel vm)
    {
        if (vm.IsInstalled)
        {
            return;
        }

        var stream = await BaseUrl.AppendPathSegment("/api/v1/extensions/install")
                                  .PostJsonAsync(new
                                  {
                                      apk = vm.Apk,
                                      repoUrl = Settings.Repository
                                  })
                                  .ReceiveStream();
        var doc = await JsonDocument.ParseAsync(stream);
        try
        {
            if (doc.RootElement.GetProperty("ok").GetBoolean())
            {
                vm.IsInstalled = true;
            }
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    private async Task UninstallExtension(MiwayomiProviderViewModel vm)
    {
        if (!vm.IsInstalled)
        {
            return;
        }

        var stream = await BaseUrl.AppendPathSegment("/api/v1/extensions/uninstall")
                                  .PostJsonAsync(new
                                  {
                                      pkg = vm.Package
                                  })
                                  .ReceiveStream();
        var doc = await JsonDocument.ParseAsync(stream);
        try
        {
            if (doc.RootElement.GetProperty("ok").GetBoolean())
            {
                vm.IsInstalled = false;
            }
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    private async Task AddRepository()
    {
        var options = new ModuleOptions();
        options.AddOption(b => b.WithName("Name"))
               .AddOption(b => b.WithName("Url"));

        var result = await dialogService.EditModuleOptions("Add Repository", options);

        if (!result)
        {
            return;
        }

        var repository = OverridableConfig.CreateFrom<RepositoryDescriptor>(options);
        Repositories.Add(repository);
        Settings.Repositories.Add(repository);
        Save();
    }
    
}