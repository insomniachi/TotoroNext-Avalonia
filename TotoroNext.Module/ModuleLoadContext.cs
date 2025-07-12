using System.Reflection;
using System.Runtime.Loader;
using Avalonia;
using Avalonia.Platform;

namespace TotoroNext.Module;

public class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModuleLoadContext(string path) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(path);
        Unloading += sender =>
        {
            AvaloniaPropertyRegistry.Instance.UnregisterByModule(sender.Assemblies.First().DefinedTypes);
            AssetLoader.InvalidateAssemblyCache(sender.Assemblies.First().GetName().Name!);
        };
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath == null)
        {
            return null;
        }

        if (assemblyPath.EndsWith("WinRT.Runtime.dll") || assemblyPath.EndsWith("Microsoft.Windows.SDK.NET.dll") ||
            assemblyPath.EndsWith("Avalonia.Controls.dll") || assemblyPath.EndsWith("Avalonia.Base.dll") ||
            assemblyPath.EndsWith("Avalonia.Markup.Xaml.dll"))
        {
            return null;
        }

        return LoadFromAssemblyPath(assemblyPath);

    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}