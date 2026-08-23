using CommunityToolkit.Mvvm.Input;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Local.Mapping;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Local.ViewModels;

internal partial class SettingsViewModel(
    IDbContext dbContext,
    IDialogService dialogService,
    ILocalTrackingService localTrackingService,
    IFactory<ITrackingService, Guid> trackingServiceFactory,
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings,
    IEnumerable<Descriptor> descriptors) : ModuleSettingsViewModel<Settings>(settings)
{
    [RelayCommand]
    private async Task DownloadAllCachedSeasons()
    {
        await dbContext.DownloadAllSeasonsFromCache();
    }

    [RelayCommand]
    private async Task DownloadCachedSeason()
    {
        if (await GetSeasonFromUser() is not { } input)
        {
            return;
        }

        await dbContext.DownloadSeasonFromCache(input.Year, input.SeasonName);
    }

    [RelayCommand]
    private async Task DownloadSeason()
    {
        if (await GetSeasonFromUser() is not { } input)
        {
            return;
        }

        await dbContext.DownloadSeason(input.Year, input.SeasonName);
    }

    [RelayCommand]
    private async Task ImportList()
    {
        var trackingServices = descriptors.Where(x => x.Id != Module.Id)
                                          .Where(x => x.Components.Contains(ComponentTypes.Tracking))
                                          .ToList();
        var names = trackingServices.Select(x => x.Name);
        var options = new DataContainer();
        options.WithProperty(b => b.WithName("Service").WithAllowedValues(names));

        var result = await dialogService.EditDataContainer("Select Service", options);
        if (!result)
        {
            return;
        }

        var selectedService = options.GetString("Service");
        if (trackingServices.FirstOrDefault(x => x.Name == selectedService) is not { } descriptor)
        {
            return;
        }

        var service = trackingServiceFactory.Create(descriptor.Id);

        if (service is null)
        {
            return;
        }

        var userlist = await service.GetUserList(CancellationToken.None);
        await Task.Run(() => localTrackingService.SyncList(userlist));
    }

    [RelayCommand]
    private async Task DownloadAnnDump()
    {
        var ann = new AnimeNewsNetwork(httpClientFactory);
        await ann.DownloadDump();
    }

    [RelayCommand]
    private async Task DownloadAnidbDump()
    {
        var ann = new Anidb(httpClientFactory);
        await ann.DownloadDump();
    }

    private async Task<(int Year, AnimeSeason SeasonName)?> GetSeasonFromUser()
    {
        var options = new DataContainer();
        var current = AnimeHelpers.CurrentSeason();
        options.WithProperty(b => b.WithValueAndName(current.Year));
        options.WithProperty(b => b.WithValueAndName(current.SeasonName));

        var result = await dialogService.EditDataContainer("Select Season", options);
        if (!result)
        {
            return null;
        }

        var year = options.GetInt32(nameof(current.Year));
        var season = options.GetValue<AnimeSeason>(nameof(current.SeasonName));
        
        return (year, season);
    }
}