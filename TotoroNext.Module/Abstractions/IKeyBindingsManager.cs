using Microsoft.Extensions.Hosting;

namespace TotoroNext.Module.Abstractions;

public interface IKeyBindingsManager : IHostedService
{
    void AddProvider(IKeyBindingsProvider provider);
    void RemoveProvider(IKeyBindingsProvider provider);
}