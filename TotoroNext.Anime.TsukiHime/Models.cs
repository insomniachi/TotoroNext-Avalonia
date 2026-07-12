using System.Text.Json.Serialization;

namespace TotoroNext.Anime.TsukiHime;

[Serializable]
public class GroupsListResponse : BaseListResponse<GroupDescriptor>;

[Serializable]
public class EpisodeTorrentsListResponse : BaseListResponse<TorrentDescriptor>;

[Serializable]
public class BaseListResponse<T>
{
    [JsonPropertyName("total")] public int Total { get; set; }

    [JsonPropertyName("results")] public List<T> Results { get; set; } = [];
}

[Serializable]
public class GroupDescriptor
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("id")] public int Id { get; set; }
}

[Serializable]
public class TorrentDescriptor
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("group")] public GroupDescriptor Group { get; set; } = new();
}

[Serializable]
public class TorrentContent
{
    [JsonPropertyName("size")] public long Size { get; set; }

    [JsonPropertyName("links")] public DirectLinks Links { get; set; } = new();
}

[Serializable]
public class DirectLinks
{
    public string FileDitch { get; set; } = "";
    public string Gofile { get; set; } = "";
    public string BuzzHeavier { get; set; } = "";
}