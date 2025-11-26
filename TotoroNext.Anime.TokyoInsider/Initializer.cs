using System.Text.Json;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TokyoInsider;

public class Initializer : IInitializer, IBackgroundInitializer
{
    public void Initialize()
    {
        var filePath = ModuleHelper.GetFilePath(Module.Descriptor, "catalog.json");
        if (!File.Exists(filePath))
        {
            return;
        }

        var contents = File.ReadAllText(filePath);
        Catalog.Items = JsonSerializer.Deserialize<List<TokyoInsiderItem>>(contents) ?? [];
    }

    public async Task BackgroundInitializeAsync()
    {
        var filePath = ModuleHelper.GetFilePath(Module.Descriptor, "catalog.json");
        if (File.Exists(filePath))
        {
            return;
        }

        await Catalog.DownloadCatalog();
    }
}