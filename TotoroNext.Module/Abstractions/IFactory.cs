namespace TotoroNext.Module.Abstractions;

public interface IFactory<out TService, TId>
    where TService : notnull
{
    TService? Create(TId? id);

    TService? CreateDefault();

    IEnumerable<TService> CreateAll();

    TId? GetDefaultId();

    bool CanCreate();
}