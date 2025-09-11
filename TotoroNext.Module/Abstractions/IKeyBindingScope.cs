using Avalonia.Input;

namespace TotoroNext.Module.Abstractions;

public interface IKeyBindingScope
{
    List<KeyBinding> KeyBindings { get; }
    void SetScope(IKeyBindingsProvider provider);
    void ClearScope();
}

public interface IKeyBindingsProvider
{
    IEnumerable<KeyBinding> GetKeyBindings();
}