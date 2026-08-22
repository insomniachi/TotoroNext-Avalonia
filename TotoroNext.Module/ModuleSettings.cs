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
        Descriptor = descriptor;

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
    
    public Descriptor Descriptor { get; }

    public void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(Value, Options));
    }
}

public abstract class ModuleSettingsViewModel<TSettings>(IModuleSettings<TSettings> data) : ObservableObject, IModuleSettingsViewModel
    where TSettings : class, new()
{
    protected TSettings Settings => data.Value;
    
    public Descriptor Descriptor => data.Descriptor;

    public DataContainer? EditableSettings { get; private set; }

    public virtual void Initialize()
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
    
    protected void Save() => data.Save();
}

public interface IModuleSettingsViewModel : IInitializable
{
    DataContainer? EditableSettings { get; }
    Descriptor Descriptor { get; }
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