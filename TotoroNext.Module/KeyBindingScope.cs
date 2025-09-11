using Avalonia.Input;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class KeyBindingScope : IKeyBindingScope
{
    public List<KeyBinding> KeyBindings { get; } = [];
    
    public void SetScope(IKeyBindingsProvider provider)
    {
        KeyBindings.AddRange(provider.GetKeyBindings());
    }
    
    public void ClearScope()
    {
        KeyBindings.Clear();
    }
}