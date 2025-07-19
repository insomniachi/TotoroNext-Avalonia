namespace PluginBuilder;

public class PluginProject(string name)
{
    public string Name { get; } = name;
    public string[] Dependencies { get; init; } = [];
}