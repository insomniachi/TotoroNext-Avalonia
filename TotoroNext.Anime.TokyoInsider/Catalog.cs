using System.Text.Json;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Module;

namespace TotoroNext.Anime.TokyoInsider;

public static class Catalog
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    internal static List<TokyoInsiderItem> Items { get; set; } = [];
    
    public static async Task DownloadCatalog()
    {
        var stream = await "https://www.tokyoinsider.com/anime/list".GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var catalog = new List<TokyoInsiderItem>();
        foreach (var node in doc.QuerySelectorAll(".c_h2,.c_h2b"))
        {
            var link = node.QuerySelector("a");
            if (link is null)
            {
                continue;
            }
            
            catalog.Add(new TokyoInsiderItem(link.GetAttributeValue("href", ""), link.InnerHtml));
        }
        
        var file = FileHelper.GetModulePath(Module.Descriptor, "catalog.json");
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Items = catalog;
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(catalog, Options));
    }
}

[Serializable]
internal class TokyoInsiderItem(string id, string title)
{
    public string Id { get; set; } = id;
    public string Title { get; set; } = title;
}