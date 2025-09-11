using Avalonia.Input;

namespace TotoroNext.Module.Abstractions;

public interface IKeyBindingsManager
{
    List<KeyBinding> KeyBindings { get; }
    void SetActiveBindings(IKeyBindingsProvider provider);
    void ResetBindings();
}