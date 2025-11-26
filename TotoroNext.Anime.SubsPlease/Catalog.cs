using System.Text.Json;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Module;

namespace TotoroNext.Anime.SubsPlease;

internal static class Catalog
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    internal static List<SubsPleaseItem> Items { get; set; } = [];

    internal static async Task DownloadCatalog()
    {
        var stream = await "https://subsplease.org/shows/".GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var catalog = new List<SubsPleaseItem>();
        foreach (var link in doc.QuerySelectorAll(".all-shows-link a"))
        {
            var id = link.GetAttributeValue("href", "").Replace("/shows/", "");
            var title = link.GetAttributeValue("title", "");

            catalog.Add(new SubsPleaseItem(id, title));
        }

        var file = ModuleHelper.GetFilePath(Module.Descriptor, "catalog.json");
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Items = catalog;
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(catalog, Options));
    }

    [Serializable]
    internal class SubsPleaseItem(string id, string title)
    {
        public string Id { get; set; } = id;
        public string Title { get; set; } = title;
    }
}