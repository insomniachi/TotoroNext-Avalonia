using System.IO.Compression;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl;
using TotoroNext.Module.Abstractions;
using Path = System.IO.Path;

namespace TotoroNext.Module;

public class ModuleStore : IModuleStore
{
    private readonly HttpClient _client = new();
    private readonly string _url = "https://raw.githubusercontent.com/insomniachi/TotoroNext/refs/heads/master/manifest.json";
    private readonly string _modulesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "Modules");
    private readonly List<AssemblyLoadContext> _contexts = [];

    public async IAsyncEnumerable<IModule> LoadModules()
    {
        var manifests = await GetAllModules().ToListAsync();

#if WINDOWS
        var targetFrameworkPattern = "*net9.0-windows10.0.26100";
#else
        var targetFrameworkPattern = "*net9.0-desktop";
#endif

        var directories = Directory.GetDirectories(_modulesPath, targetFrameworkPattern, SearchOption.AllDirectories);

        foreach (var item in directories.SelectMany(x => Directory.GetFiles(x, "*.dll", SearchOption.AllDirectories)))
        {
            var fileName = Path.GetFileName(item);

            if (!manifests.Any(x => x.EntryPoint == fileName))
            {
                continue;
            }

            var context = new ModuleLoadContext(item);
            _contexts.Add(context);
            var assembly = context.LoadFromAssemblyPath(item);
            var modules = assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IModule)) && !x.IsAbstract).ToList();

            if (modules.Count == 0)
            {
                context.Unload();
                continue;
            }

            foreach (var moduleType in modules)
            {
                yield return (IModule)Activator.CreateInstance(moduleType)!;
            }
        }
    }

    public async Task<bool> DownloadModule(ModuleManifest manifest)
    {
        try
        {
#if WINDOWS
            var targetFramework = "net9.0-windows10.0.26100";
#else
            var targetFramework = "net9.0-desktop";
#endif
            var destination = Path.Combine(_modulesPath, manifest.EntryPoint.Replace(".dll", ""), targetFramework);
            var downloadUrl = Url.Combine(manifest.Versions[0].SourceUrl, targetFramework + ".zip");
            var stream = await _client.GetStreamAsync(downloadUrl);
            ZipFile.ExtractToDirectory(stream, destination, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async IAsyncEnumerable<ModuleManifest> GetAllModules()
    {
        var response = await _client.GetStringAsync(_url);
        var array = JsonNode.Parse(response)?.AsArray() ?? throw new InvalidOperationException("Failed to parse module manifest.");

        var manifests = array.Deserialize<List<ModuleManifest>>();

        foreach (var item in manifests ?? [])
        {
            yield return item;
        }
    }
}
