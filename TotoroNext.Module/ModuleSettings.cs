using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Module.Abstractions;
using Path = System.IO.Path;

namespace TotoroNext.Module;

public class ModuleSettings<TData> : IModuleSettings<TData>
    where TData : class, new()
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _filePath;

    public ModuleSettings(Descriptor descriptor)
    {
        _filePath = FileHelper.GetModulePath(descriptor, "settings.json");

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

    public TData Value { get; }

    public void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Value, Options));
    }
}

public abstract class ModuleSettingsViewModel<TSettings>(IModuleSettings<TSettings> data) : ObservableObject
    where TSettings : class, new()
{
    protected TSettings Settings => data.Value;

    public ModuleOptions? EditableSettings { get; private set; }

    public void Initialize()
    {
        if (Settings is not OverridableConfig oc)
        {
            return;
        }

        EditableSettings = oc.ToModuleOptions();
        OnPropertyChanged(nameof(EditableSettings));

        foreach (var item in EditableSettings)
        {
            item.PropertyChanged += (_, _) =>
            {
                oc.UpdateValues(EditableSettings);
                data.Save();
            };
        }
    }

    protected void SetAndSaveProperty<TProperty>(ref TProperty field, TProperty value, Action<TSettings> settingUpdate,
                                                 [CallerMemberName] string propertyName = "")
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

public static class FileHelper
{
    public static string GetModulePath(Descriptor descriptor, string fileName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "TotoroNext",
                            "Modules",
                            descriptor.EntryPoint,
                            fileName);
    }

    public static string GetPath(string fileName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "TotoroNext",
                            fileName);
    }
}