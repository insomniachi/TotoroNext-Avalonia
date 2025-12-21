using System.IO.Compression;
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
            var modules = new List<IModule>();
            try
            {
                var assembly = context.LoadFromAssemblyPath(item);
                modules.AddRange(assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IModule)) && !x.IsAbstract)
                                         .Select(moduleType => (IModule)Activator.CreateInstance(moduleType)!));

                if (modules.Count == 0)
                {
                    context.Unload();
                    continue;
                }

                _contexts.Add(context);
            }
            catch
            {
                continue;
            }

            foreach (var module in modules)
            {
                yield return module;
            }
        }
    }

    public async Task<bool> DownloadModule(ModuleManifest manifest)
    {
        var targetDir = Path.Combine(_modulesPath, manifest.EntryPoint.Replace(".dll", ""));
        try
        {
            var downloadUrl = manifest.Versions[0].SourceUrl;
            var stream = await _client.GetStreamAsync(downloadUrl);
            await using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
            foreach (var entry in archive.Entries)
            {
                // Skip empty entries
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                // Remove root folder from path
                var parts = entry.FullName.Split('/', '\\');
                var trimmedPath = Path.Combine(parts.Length > 1 ? string.Join(Path.DirectorySeparatorChar.ToString(), parts[1..]) : entry.Name);

                var destinationPath = Path.Combine(targetDir, trimmedPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                await entry.ExtractToFileAsync(destinationPath, true);
            }

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