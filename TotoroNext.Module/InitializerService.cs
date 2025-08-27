using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class InitializerService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var tasks = scope.ServiceProvider
                         .GetServices<IBackgroundInitializer>()
                         .Select(service => service.BackgroundInitializeAsync());

        return Task.WhenAll(tasks);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}