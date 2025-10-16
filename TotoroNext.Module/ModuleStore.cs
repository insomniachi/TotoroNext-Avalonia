using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using TotoroNext.Module.Abstractions;
using Path = System.IO.Path;

namespace TotoroNext.Module;

public class ModuleStore : IModuleStore
{
    private const string Url = "https://raw.githubusercontent.com/insomniachi/TotoroNext-Avalonia/refs/heads/master/manifest.json";
    private readonly HttpClient _client = new();

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<AssemblyLoadContext> _contexts = [];

    private readonly string _modulesPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "Modules");

    public IEnumerable<IModule> LoadModules()
    {
        if (!Directory.Exists(_modulesPath))
        {
            yield break;
        }
        
        var directories = Directory.GetDirectories(_modulesPath, "*", SearchOption.AllDirectories);

        foreach (var item in directories.SelectMany(x => Directory.GetFiles(x, "*.dll", SearchOption.AllDirectories)))
        {
            var fileName = Path.GetFileNameWithoutExtension(item);
            var directoryName = Path.GetFileName(Path.GetDirectoryName(item));

            if (fileName != directoryName)
            {
                continue;
            }

            var context = new ModuleLoadContext(item);
            Assembly assembly;
            try
            {
                assembly = context.LoadFromAssemblyPath(item);
            }
            catch
            {
                continue;
            }

            var modules = assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IModule)) && !x.IsAbstract).ToList();

            if (modules.Count == 0)
            {
                context.Unload();
                continue;
            }

            _contexts.Add(context);

            foreach (var module in modules.Select(moduleType => (IModule)Activator.CreateInstance(moduleType)!))
            {
                yield return module;
            }
        }
    }

    public async Task<bool> DownloadModule(ModuleManifest manifest)
    {
        try
        {
            var destination = Path.Combine(_modulesPath, manifest.EntryPoint.Replace(".dll", ""));
            var downloadUrl = Flurl.Url.Combine(manifest.Versions[0].SourceUrl, ".zip");
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
        var response = await _client.GetStringAsync(Url);
        var array = JsonNode.Parse(response)?.AsArray() ?? throw new InvalidOperationException("Failed to parse module manifest.");

        var manifests = array.Deserialize<List<ModuleManifest>>();

        foreach (var item in manifests ?? [])
        {
            yield return item;
        }
    }
}