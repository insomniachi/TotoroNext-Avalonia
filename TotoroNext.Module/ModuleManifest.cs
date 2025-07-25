namespace TotoroNext.Module;

public class ModuleManifest
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string EntryPoint { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Owner { get; set; }
    public string[] Categories { get; set; } = [];
    public VersionInfo[] Versions { get; set; } = [];
}

public class VersionInfo
{
    public required string Version { get; set; }
    public required string TargetVersion { get; set; }
    public required string SourceUrl { get; set; }
    public string? Changelong { get; set; }
}