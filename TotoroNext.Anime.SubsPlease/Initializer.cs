using System.Text.Json;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.SubsPlease;

public class Initializer : IInitializer, IBackgroundInitializer
{
    public void Initialize()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "catalog.json");
        if (!File.Exists(filePath))
        {
            return;
        }

        var contents = File.ReadAllText(filePath);
        Catalog.Items = JsonSerializer.Deserialize<List<Catalog.SubsPleaseItem>>(contents) ?? [];
    }

    public async Task BackgroundInitializeAsync()
    {
        var filePath = FileHelper.GetModulePath(Module.Descriptor, "catalog.json");
        if (File.Exists(filePath))
        {
            return;
        }

        await Catalog.DownloadCatalog();
    }
}