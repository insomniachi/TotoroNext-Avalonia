namespace TotoroNext.Module.Abstractions;

public interface IFactory<TService, TId>
    where TService : notnull
{
    TService Create(TId id);

    TService CreateDefault();

    IEnumerable<TService> CreateAll();

    bool CanCreate();
}
