using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Module;

public partial class ModuleOptionItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public SpecialEditorType EditorType { get; init; } = SpecialEditorType.TextBox;
    [ObservableProperty] public partial string Value { get; set; } = "";
    public IEnumerable<string>? AllowedValues { get; init; }

    public bool IsChecked
    {
        get => Value == bool.TrueString;
        set => Value = value.ToString();
    }

    public T GetValueOrDefault<T>(Func<string, T> parser, T defaultValue)
    {
        try
        {
            return parser(Value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public string GetString(string name, string defaultValue)
    {
        return Value;
    }

    public bool GetBool(string name, bool defaultValue)
    {
        return Value == bool.TrueString;
    }

    public int GetInt32(string name, int defaultValue)
    {
        return GetValueOrDefault(int.Parse, defaultValue);
    }

    public double GetDouble(string name, double defaultValue)
    {
        return GetValueOrDefault(double.Parse, defaultValue);
    }

    public TEnum GetEnum<TEnum>(string name, TEnum defaultValue) where TEnum : Enum
    {
        return GetValueOrDefault(x => (TEnum)Enum.Parse(typeof(TEnum), x), defaultValue);
    }

    public object GetEnum(Type enumType, string name, object defaultValue)
    {
        return GetValueOrDefault(s => Enum.Parse(enumType, s), defaultValue);
    }
}

public class ModuleOptions(IEnumerable<ModuleOptionItem> items) : List<ModuleOptionItem>([.. items])
{
    public ModuleOptions() : this([]) { }

    public ModuleOptions AddOption(Action<ModuleOptionBuilder> creator)
    {
        var builder = new ModuleOptionBuilder();
        creator(builder);
        Add(builder.ToPluginOption());
        return this;
    }

    public ModuleOptionItem? GetOptionOrDefault(string name) => this.FirstOrDefault(x => x.Name == name);

    public string GetString(string name, string defaultValue = "") => GetOptionOrDefault(name)?.Value ?? defaultValue;

    public bool GetBool(string name, bool defaultValue = false) =>
        GetOptionOrDefault(name) is not { } option ? defaultValue : option.Value == bool.TrueString;

    public int GetInt32(string name, int defaultValue = 0) => GetOptionOrDefault(name)?.GetInt32(name, defaultValue) ?? defaultValue;

    public double GetDouble(string name, double defaultValue = 0) => GetOptionOrDefault(name)?.GetDouble(name, defaultValue) ?? defaultValue;

    public TEnum GetEnum<TEnum>(string name, TEnum defaultValue) where TEnum : Enum
    {
        var option = GetOptionOrDefault(name);
        return option is null ? defaultValue : option.GetValueOrDefault(x => (TEnum)Enum.Parse(typeof(TEnum), x), defaultValue);
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class SpecialEditorTypeAttribute(SpecialEditorType type) : Attribute
{
    public SpecialEditorType Type { get; } = type;
}

public enum SpecialEditorType
{
    TextBox,
    ComboBox,
    ToggleSwitch,
    FileBrowser
}