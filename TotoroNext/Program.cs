using Avalonia;
using Avalonia.Markup.Declarative;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using Velopack;

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
        try
        {
            VelopackApp.Build()
                       .OnFirstRun(_ =>
                       {
                           /* Your first run code here */
                       })
                       .Run();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            var message = "Unhandled exception: " + e;
            Console.WriteLine(message);
            throw;
        }
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