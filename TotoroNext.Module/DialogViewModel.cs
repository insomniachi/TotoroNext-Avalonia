using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public abstract partial class DialogViewModel : ObservableObject, IKeyBindingsProvider, IDialogContext
{
    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public virtual IEnumerable<KeyBinding> GetKeyBindings()
    {
        return
        [
            new KeyBinding
            {
                Gesture = new KeyGesture(Key.Escape),
                Command = CloseCommand
            },
            ..GetExtraKeyBindings()
        ];
    }

    protected virtual IEnumerable<KeyBinding> GetExtraKeyBindings()
    {
        yield break;
    }
}