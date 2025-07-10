using System.Reflection;
using System.Runtime.Loader;

namespace TotoroNext.Module;

public class ModuleLoadContext(string path) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver _resolver = new(path);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
