using Avalonia.Input;

namespace TotoroNext.Module.Abstractions;

public interface IKeyBindingsProvider
{
    IEnumerable<KeyBinding> GetKeyBindings();
}