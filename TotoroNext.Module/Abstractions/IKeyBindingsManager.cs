using Microsoft.Extensions.Hosting;

namespace TotoroNext.Module.Abstractions;

public interface IKeyBindingsManager : IHostedService
{
    void SetActiveBindings(IKeyBindingsProvider provider);
    void ResetBindings();
}