namespace TotoroNext.Anime.Abstractions.Models;

public sealed class AnimeOverrides
{
    public bool IsNsfw { get; set; }
    public Guid? Provider { get; set; }

    public string? SelectedResult { get; set; }
    public event EventHandler? Reverted;

    public void Revert()
    {
        Reverted?.Invoke(this, EventArgs.Empty);
    }
}