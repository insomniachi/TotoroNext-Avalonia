using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class Factory<TService, TId>(
    IServiceScopeFactory serviceScopeFactory,
    ILocalSettingsService localSettingsService,
    string defaultKey) : IFactory<TService, TId>
    where TService : notnull
{
    public TService Create(TId? id)
    {
        if (id is null)
        {
            ArgumentNullException.ThrowIfNull(id, nameof(id));
        }

        using var scope = serviceScopeFactory.CreateScope();
        return scope.ServiceProvider.GetKeyedService<TService>(id) ?? scope.ServiceProvider.GetRequiredKeyedService<TService>(Guid.Empty);
    }

    public TService? CreateDefault()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var key = localSettingsService.ReadSetting<TId>(defaultKey);
        return scope.ServiceProvider.GetKeyedService<TService>(key) ?? scope.ServiceProvider.GetKeyedServices<TService>(KeyedService.AnyKey).FirstOrDefault();
    }

    public IEnumerable<TService> CreateAll()
    {
        using var scope = serviceScopeFactory.CreateScope();
        return scope.ServiceProvider.GetKeyedServices<TService>(KeyedService.AnyKey);
    }

    public bool CanCreate()
    {
        return CreateAll().Any();
    }
}