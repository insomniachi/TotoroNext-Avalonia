using System.Collections.ObjectModel;
using System.Text.Json;
using DynamicData;
using Flurl.Http;
using TotoroNext.Module;

namespace TotoroNext.Anime.TsukiHime;

public static class TsukiHimeGroups
{
    public static ObservableCollection<GroupDescriptor> Groups { get; set; } = [];
    public static Descriptor Descriptor { get; set; } = null!;

    public static async Task FetchAsync(IFlurlClient client)
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

        } while (offset != total);
        
        var path = FileHelper.GetModulePath(Descriptor, "groups.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(Groups));
    }

    public static void LoadGroups()
    {
        var filePath = FileHelper.GetModulePath(Descriptor, "groups.json");
        if (!File.Exists(filePath))
        {
            return;
        }
        
        Groups = JsonSerializer.Deserialize<ObservableCollection<GroupDescriptor>>(File.ReadAllText(filePath)) ?? [];
    }
}