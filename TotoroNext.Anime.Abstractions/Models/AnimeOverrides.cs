namespace TotoroNext.Anime.Abstractions.Models;

public sealed partial class AnimeOverrides
{
    public event EventHandler? Reverted;
    public bool IsNsfw { get; set; }
    public Guid? Provider { get; set; }

    public void Revert()
    {
        Reverted?.Invoke(this, EventArgs.Empty);
    }
}
