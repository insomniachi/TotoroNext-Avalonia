using System.Collections.ObjectModel;
using System.Text.Json;
using DynamicData;
using Flurl.Http;
using Infidex;
using Infidex.Api;
using Infidex.Core;
using TotoroNext.Module;

namespace TotoroNext.Anime.TsukiHime;

public static class TsukiHimeLocalData
{
    private static SearchEngine? _engine;
    public static ObservableCollection<GroupDescriptor> Groups { get; set; } = [];
    public static Descriptor Descriptor { get; set; } = null!;

    public static async Task FetchGroupsAsync(IFlurlClient client)
    {
        Groups.Clear();
        const int limit = 100;
        var total = 0;
        var offset = 0;
        do
        {
            var response = await client.Request("groups")
                                       .AppendQueryParam("limit", limit)
                                       .AppendQueryParam("offset", offset)
                                       .GetJsonAsync<GroupsListResponse>();
            total = response.Total;
            offset += response.Results.Count;
            Groups.AddRange(response.Results);

            await Task.Delay(100);
        } while (offset != total);

        var path = FileHelper.GetModulePath(Descriptor, "groups.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(Groups));
    }

    public static async Task FetchAnimeAsync(IFlurlClient client)
    {
        _engine = SearchEngine.CreateDefault();
        var documents = new List<Document>();
        var total = 0;
        var offset = 0;
        var counter = 0;
        do
        {
            try
            {
                var response = await client.Request("animes")
                                           .AppendQueryParam("limit", 100)
                                           .AppendQueryParam("offset", offset)
                                           .AppendQueryParam("sort", "alphabetical")
                                           .AppendQueryParam("order", "asc")
                                           .GetJsonAsync<AnimeListResponse>();
                total = response.Total;
                offset += response.Results.Count;

                foreach (var item in response.Results)
                {
                    var fields = new DocumentFields();
                    fields.AddField("Id", item.Id, Weight.Low);
                    fields.AddField("Title", item.Title, Weight.High);
                    documents.Add(new Document(counter++, fields));
                }

                await Task.Delay(100);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        } while (offset != total);

        var file = FileHelper.GetModulePath(Descriptor, "catalog.bin");
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

    internal static IEnumerable<AnimeDescriptor> Search(string query)
    {
        if (_engine is null)
        {
            yield break;
        }

        var q = new Query
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
            if (document is null or { Fields: null })
            {
                continue;
            }

            var id = 0;
            var title = "";
            if (document.Fields.GetField("content") is { } contentField)
            {
                var content = contentField.Value as string;
                var parts = content!.Split('§');
                title = parts[0];
                id = int.Parse(parts[1]);
            }
            else if (document.Fields.GetField("Id") is { } idField &&
                     document.Fields.GetField("Title") is { } titleField)
            {
                id = int.Parse(idField.Value?.ToString() ?? "0");
                title = titleField.Value as string;
            }

            if (title == null)
            {
                continue;
            }

            yield return new AnimeDescriptor
            {
                Id = id,
                Title = title
            };
        }
    }

    public static void LoadData()
    {
        LoadEngine();
        LoadGroups();
    }

    internal static void LoadGroups()
    {
        var filePath = FileHelper.GetModulePath(Descriptor, "groups.json");
        if (!File.Exists(filePath))
        {
            return;
        }

        Groups = JsonSerializer.Deserialize<ObservableCollection<GroupDescriptor>>(File.ReadAllText(filePath)) ?? [];
    }

    internal static void LoadEngine()
    {
        var filePath = FileHelper.GetModulePath(Descriptor, "catalog.bin");
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            _engine = SearchEngine.Load(filePath, [400], wordMatcherSetup: new WordMatcherSetup());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}