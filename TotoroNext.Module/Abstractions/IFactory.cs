namespace TotoroNext.Module.Abstractions;

public interface IFactory<out TService, in TId>
    where TService : notnull
{
    TService Create(TId id);

    TService? CreateDefault();

    IEnumerable<TService> CreateAll();

    bool CanCreate();
}