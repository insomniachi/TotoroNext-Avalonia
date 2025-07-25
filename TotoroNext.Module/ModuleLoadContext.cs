using System.Reflection;
using System.Runtime.Loader;

namespace TotoroNext.Module;

public class ModuleLoadContext(string path) : AssemblyLoadContext(false)
{
    private readonly AssemblyDependencyResolver _resolver = new(path);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is not null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}