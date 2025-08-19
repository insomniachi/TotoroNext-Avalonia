namespace TotoroNext.Module.Abstractions;

public interface IBackgroundInitializer
{
    Task BackgroundInitializeAsync();
}

public interface IInitializer
{
    void Initialize();
}