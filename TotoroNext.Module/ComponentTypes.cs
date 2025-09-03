namespace TotoroNext.Module;

public static class ComponentTypes
{
    public const string AnimeProvider = "Anime Provider";
    public const string MediaEngine = "Media Engine";
    public const string Metadata = "Metadata";
    public const string Tracking = "Tracking";
    public const string MediaSegments = "Segments";
    public const string AnimeDownloader = "Anime Downloader";
    public const string Debrid = "Debrid";
}

public interface IComponentRegistry
{
    void RegisterComponent(string componentType, Descriptor descriptor);
    IEnumerable<Descriptor> GetComponents(string componentType);
}

public class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<string, List<Descriptor>> _components = [];

    public void RegisterComponent(string componentType, Descriptor descriptor)
    {
        if (_components.TryGetValue(componentType, out var list))
        {
            list.Add(descriptor);
        }
        else
        {
            _components[componentType] = [descriptor];
        }
    }

    public IEnumerable<Descriptor> GetComponents(string componentType)
    {
        return _components.TryGetValue(componentType, out var descriptors) ? descriptors : Enumerable.Empty<Descriptor>();
    }
}