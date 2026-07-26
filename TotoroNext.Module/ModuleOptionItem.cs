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

public class ModuleOptions(IEnumerable<ModuleOptionItem> items) : List<ModuleOptionItem>(items.ToList())
{
    public ModuleOptions() : this([]) { }

    public ModuleOptions AddOption(Func<ModuleOptionBuilder, ModuleOptionItem> creator)
    {
        var builder = new ModuleOptionBuilder();
        Add(creator(builder));
        return this;
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
    NumberBox,
    ToggleSwitch,
    FileBrowser
}