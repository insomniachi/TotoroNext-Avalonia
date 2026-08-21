using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Local.Views;

internal partial class SettingsViewModel(IDbContext dbContext,
                                         IDialogService dialogService,
                                         ILocalTrackingService localTrackingService,
                                         IFactory<ITrackingService, Guid> trackingServiceFactory,
                                         IEnumerable<Descriptor> descriptors) : ObservableObject
{
    [RelayCommand]
    private async Task DownloadAllCachedSeasons()
    {
        await dbContext.DownloadAllSeasonsFromCache();
    }

    [RelayCommand]
    private async Task DownloadCachedSeason()
    {
        var options = new ModuleOptions();
        options.AddOption(b => b.WithName("Year"));
        options.AddOption(b => b.WithDisplayName("Season").WithAllowedValues<AnimeSeason>());

        var result = await dialogService.EditModuleOptions("Select Season", options);
        if (!result)
        {
            return;
        }

        var year = options.GetInt32("Year");
        var season = options.GetEnum("SeasonName", AnimeSeason.Winter);

        await dbContext.DownloadSeasonFromCache(year, season);
    }
    
    [RelayCommand]
    private async Task DownloadSeason()
    {
        var current = AnimeHelpers.CurrentSeason();
        
        var options = new ModuleOptions();
        options.AddOption(b => b.WithNameAndValue(current.Year));
        options.AddOption(b => b.WithNameAndValue(current.SeasonName)
                                .WithAllowedValues<AnimeSeason>());

        var result = await dialogService.EditModuleOptions("Select Season", options);
        if (!result)
        {
            return;
        }

        var year = options.GetInt32("Year");
        var season = options.GetEnum("SeasonName", AnimeSeason.Winter);

        await dbContext.DownloadSeason(year, season);
    }
    
    [RelayCommand]
    private async Task SyncList()
    {
        var trackingServices = descriptors.Where(x => x.Id != Module.Descriptor.Id)
                                          .Where(x => x.Components.Contains(ComponentTypes.Tracking))
                                          .ToList();
        var names = trackingServices.Select(x => x.Name);
        var options = new ModuleOptions();
        options.AddOption(b => b.WithName("Service").WithAllowedValues(names));
        
        var result = await dialogService.EditModuleOptions("Select Service", options);
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
}