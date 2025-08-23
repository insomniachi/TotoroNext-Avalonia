using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class InitializerService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var tasks = scope.ServiceProvider
                        .GetServices<IBackgroundInitializer>()
                        .Select(service => service.BackgroundInitializeAsync());

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}