using Avalonia;
using Avalonia.Markup.Declarative;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;

namespace TotoroNext;

[UsedImplicitly]
internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .WithInterFont()
                         .UseReactiveUI()
                         .UseRiderHotReload()
                         .LogToTrace();
    }
}