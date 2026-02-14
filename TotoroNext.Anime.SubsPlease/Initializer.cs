using System.Text.Json;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.SubsPlease;

public class Initializer : IInitializer, IBackgroundInitializer
{
    public async Task BackgroundInitializeAsync()
    {
        await EnsureCatalogAsync();
        await EnsureScheduleAsync();
    }

    public void Initialize()
    {
        TryLoadCatalog();
        TryLoadSchedule();
    }
    
    private static async Task EnsureCatalogAsync()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "catalog.json");
        if (File.Exists(filePath))
        {
            return;
        }

        await Catalog.DownloadCatalog();
    }
    
    private static async Task EnsureScheduleAsync()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "schedule.json");
        if (File.Exists(filePath))
        {
            return;
        }

        await Schedule.DownloadSchedule();
    }

    private static void TryLoadCatalog()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "catalog.json");
        if (!File.Exists(filePath))
        {
            return;
        }

        var contents = File.ReadAllText(filePath);
        Catalog.Items = JsonSerializer.Deserialize<List<Catalog.SubsPleaseItem>>(contents) ?? [];
    }
    
    private static void TryLoadSchedule()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "schedule.json");
        if (!File.Exists(filePath))
        {
            return;
        }

        var contents = File.ReadAllText(filePath);
        var schedule = JsonSerializer.Deserialize<ScheduleResult>(contents);

        if (schedule is null)
        {
            return;
        }
        
        Schedule.Initialize(schedule);
    }
}