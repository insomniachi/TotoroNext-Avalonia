using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Infidex;
using Infidex.Api;
using Infidex.Core;
using TotoroNext.Module;

namespace TotoroNext.Anime.SubsPlease;

internal static class Catalog
{
    private static SearchEngine? _engine;

    internal static IEnumerable<SubsPleaseItem> Search(string query)
    {
        if (_engine is null)
        {
            yield break;
        }
        
        var q = new Query()
        {
            Text = query,
            MaxNumberOfRecordsToReturn = 10
        };
        
        var result = _engine.Search(q);
        var documents = result.Records
                              .Where(r => r.Score >= result.TruncationScore * 0.8f)
                              .Select(r => _engine.GetDocument(r.DocumentId));

        foreach (var document in documents)
        {
            if (document is null or {Fields: null})
            {
                continue;
            }

            var id = "";
            var title = "";
            if (document.Fields.GetField("content") is { } contentField)
            {
                var content = contentField.Value as string;
                var parts = content!.Split('§');
                title = parts[0];
                id = parts[1];
            }
            else if(document.Fields.GetField("Id") is {} idField && 
                    document.Fields.GetField("Title") is { } titleField)
            {
                id = idField.Value as string;
                title = titleField.Value as string;
            }

            if (id == null ||  title == null)
            {
                continue;
            }
            
            yield return new SubsPleaseItem(id, title);
        }   
    }

    internal static void LoadEngine(string file)
    {
        try
        {
            _engine = SearchEngine.Load(file, [400], wordMatcherSetup: new WordMatcherSetup());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    internal static async Task DownloadCatalog()
    {
        _engine = SearchEngine.CreateDefault();
        var documents = new List<Document>();
        
        var stream = await "https://subsplease.org/shows/".GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var counter = 0;
        foreach (var link in doc.QuerySelectorAll(".all-shows-link a"))
        {
            var id = link.GetAttributeValue("href", "").Replace("/shows/", "");
            var title = link.GetAttributeValue("title", "");

            var fields = new DocumentFields();
            fields.AddField("Id", id, Weight.Low);
            fields.AddField("Title", title, Weight.High);
            documents.Add(new Document(counter++, fields));
        }

        var file = FileHelper.GetModulePath(Module.Descriptor, "catalog.bin");
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _engine.IndexDocumentsAsync(documents);
        try
        {
            _engine.Save(file);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [Serializable]
    internal class SubsPleaseItem(string id, string title)
    {
        public string Id { get; set; } = id;
        public string Title { get; set; } = title;
    }
}