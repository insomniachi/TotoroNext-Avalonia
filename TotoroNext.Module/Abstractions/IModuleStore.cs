namespace TotoroNext.Module.Abstractions;

public interface IModuleStore
{
    IAsyncEnumerable<ModuleManifest> GetAllModules();
    Task<bool> DownloadModule(ModuleManifest manifest);
    IAsyncEnumerable<IModule> LoadModules();
}
