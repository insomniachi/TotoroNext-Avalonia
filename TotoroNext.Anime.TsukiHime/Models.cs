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

    [JsonPropertyName("group")] public GroupDescriptor Group { get; set; } = new();
}