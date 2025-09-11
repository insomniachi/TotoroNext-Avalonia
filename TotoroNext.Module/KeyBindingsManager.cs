using Avalonia.Input;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class KeyBindingsManager : IKeyBindingsManager
{
    public List<KeyBinding> KeyBindings { get; } = [];
    
    public void SetActiveBindings(IKeyBindingsProvider provider)
    {
        KeyBindings.AddRange(provider.GetKeyBindings());
    }
    
    public void ResetBindings()
    {
        KeyBindings.Clear();
    }
}