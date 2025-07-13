using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Module.Abstractions;
using Path = System.IO.Path;

namespace TotoroNext.Module;


internal class ModuleSettings<TData> : IModuleSettings<TData>
    where TData : class, new()
{
    private readonly string _filePath;

    internal ModuleSettings(Descriptor descriptor)
    {
        _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "Modules", descriptor.EntryPoint, $"settings.json");

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        Value = new TData();

        if (!File.Exists(_filePath))
        {
            return;
        }

        var text = File.ReadAllText(_filePath);
        if (JsonSerializer.Deserialize<TData>(text) is { } data)
        {
            Value = data;
        }
    }

    public TData Value { get; private set; }

    public void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Value));
    }
}


public abstract class ModuleSettingsViewModel<TSettings>(IModuleSettings<TSettings> data) : ObservableObject
    where TSettings : class, new()
{
    protected TSettings Settings => data.Value;

    protected void SetAndSaveProperty<TProperty>(ref TProperty field, TProperty value, Action<TSettings> settingUpdate, [CallerMemberName] string propertyName = "")
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        settingUpdate(data.Value);
        data.Save();
    }
}

public static class ResourceHelper
{
    public static Bitmap GetResource(string name)
    {
        return new
            Bitmap(AssetLoader
                       .Open(new Uri($"avares://{Assembly.GetCallingAssembly().GetName().Name}/Assets/{name}")));
    }
}
