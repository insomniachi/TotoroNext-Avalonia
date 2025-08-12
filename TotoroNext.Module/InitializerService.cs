using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class InitializerService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope =  serviceScopeFactory.CreateScope();
        foreach (var service in scope.ServiceProvider.GetServices<IInitializer>())
        {
            try
            {
                await service.InitializeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}