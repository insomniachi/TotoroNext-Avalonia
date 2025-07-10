namespace TotoroNext.Module;

public static class Container
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void SetServiceProvider(IServiceProvider services)
    {
        Services = services;
    }
}