using Avalonia;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public interface IVirtualizingBehavior<in T>
    where T : AvaloniaObject
{
    void Update(T associatedObject);
}