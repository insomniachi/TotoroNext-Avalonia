namespace TotoroNext.Module.Abstractions;

public interface IModuleSettings<out TData>
    where TData : class, new()
{
    TData Value { get; }
    void Save();
}

