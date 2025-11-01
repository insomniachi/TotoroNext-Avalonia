using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Models;

public sealed class AnimeOverrides
{
    public bool IsNsfw { get; init; }
    public Guid? Provider { get; init; }
    public string? SelectedResult { get; init; }
    public SkipMethod OpeningSkipMethod { get; init; }
    public SkipMethod EndingSkipMethod { get; init; }
    public string? SearchTerm { get; init; }
    public List<ModuleOptionItem> AnimeProviderOptions { get; init; } = [];
    public event EventHandler? Reverted;

    public void Revert()
    {
        Reverted?.Invoke(this, EventArgs.Empty);
    }
}