using Microsoft.Extensions.DependencyInjection;

namespace TotoroNext.Module.Abstractions;

public interface IModule
{
    void ConfigureServices(IServiceCollection services);
}

public interface IModule<T> : IModule
    where T : new()
{
    Descriptor Descriptor { get; }
}