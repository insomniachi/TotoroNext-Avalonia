namespace TotoroNext.Module.Abstractions;

public interface IModuleSettings<out TData>
    where TData : class, new()
{
    TData Value { get; }
    Descriptor Descriptor { get; }
    void Save();
}