using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Models;

public sealed class AnimeOverrides
{
    public bool IsNsfw { get; init; }
    public Guid? Provider { get; init; }
    public SkipMethod OpeningSkipMethod { get; init; }
    public SkipMethod EndingSkipMethod { get; init; }
    public ProviderItemResult? ProviderResult { get; set; }
    public List<ModuleOptionItem> AnimeProviderOptions { get; init; } = [];
}

[Serializable]
public class ProviderItemResult
{
    public required string Title { get; init; }
    public required string Id { get; init; }
}