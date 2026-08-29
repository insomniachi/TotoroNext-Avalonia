using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Module;

public partial class DataContainerProperty : ObservableObject
{
    public string Name { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public SpecialEditorType EditorType { get; init; } = SpecialEditorType.TextBox;
    [ObservableProperty] public partial object? Value { get; set; }
    public IEnumerable<object>? AllowedValues { get; init; }

    public T? GetValue<T>(T? defaultValue)
    {
        if (Value is T { } typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    public object? GetAsType(Type type, object? defaultValue)
    {
        return Value is null ? defaultValue : Convert.ChangeType(Value, type);
    }
}

public class DataContainer(IEnumerable<DataContainerProperty> items) : List<DataContainerProperty>([.. items])
{
    public DataContainer() : this([]) { }

    public DataContainer WithProperty(Action<DataContainerPropertyBuilder> creator)
    {
        var builder = new DataContainerPropertyBuilder();
        creator(builder);
        Add(builder.Build());
        return this;
    }

    public DataContainerProperty? GetProperty(string name)
    {
        return this.FirstOrDefault(x => x.Name == name);
    }
}

public static class DataContainerExtensions
{
    extension(DataContainerProperty item)
    {
        public string? GetString(string defaultValue = "")
        {
            return item.GetValue(defaultValue);
        }

        public bool GetBool(bool defaultValue = false)
        {
            return item.GetValue(defaultValue);
        }

        public int GetInt32(int defaultValue = 0)
        {
            return item.GetValue(defaultValue);
        }

        public double GetDouble(double defaultValue = 0)
        {
            return item.GetValue(defaultValue);
        }
    }

    extension(DataContainer options)
    {
        public string GetString(string name, string defaultValue = "")
        {
            return options.GetProperty(name)?.GetValue(defaultValue) ?? defaultValue;
        }

        public bool GetBool(string name, bool defaultValue = false)
        {
            return options.GetProperty(name)?.GetValue(defaultValue) ?? defaultValue;
        }

        public int GetInt32(string name, int defaultValue = 0)
        {
            return options.GetProperty(name)?.GetValue(defaultValue) ?? defaultValue;
        }

        public double GetDouble(string name, double defaultValue = 0)
        {
            return options.GetProperty(name)?.GetValue(defaultValue) ?? defaultValue;
        }

        public T? GetValue<T>(string name, T? defaultValue = default)
        {
            return options.GetProperty(name) is not { } option ? defaultValue : option.GetValue(defaultValue);
        }
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
    NumberBox,
    FileBrowser
}

