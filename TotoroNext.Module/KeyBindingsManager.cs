using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class KeyBindingsManager(IMessenger messenger) : IKeyBindingsManager
{
    private readonly List<KeyBinding> _keyBindings = [];

    public void SetActiveBindings(IKeyBindingsProvider provider)
    {
        _keyBindings.AddRange(provider.GetKeyBindings());
    }

    public void ResetBindings()
    {
        _keyBindings.Clear();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register<KeyGesture>(this, (_, e) =>
        {
            if (_keyBindings.FirstOrDefault(x => x.Gesture.Equals(e)) is not { } binding)
            {
                return;
            }

            binding.Command.Execute(binding.CommandParameter);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        messenger.Unregister<KeyGesture>(this);
        return Task.CompletedTask;
    }
}