using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class KeyBindingsManager(IMessenger messenger) : IKeyBindingsManager
{
    private readonly List<IKeyBindingsProvider> _providers = [];

    public void AddProvider(IKeyBindingsProvider provider)
    {
        _providers.Add(provider);
    }

    public void RemoveProvider(IKeyBindingsProvider provider)
    {
        _providers.Remove(provider);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register<KeyGesture>(this, (_, e) =>
        {
            if(_providers.SelectMany(x => x.GetKeyBindings()).LastOrDefault(x => x.Gesture.Equals(e)) is not { } binding)
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