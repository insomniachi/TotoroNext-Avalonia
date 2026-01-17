using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Module;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(SelectableModuleOptionItem), "Selectable")]
[JsonDerivedType(typeof(ToggleModuleOptionItem), "Toggle")]
public partial class ModuleOptionItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    [ObservableProperty] public partial string Value { get; set; } = "";

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

public class SelectableModuleOptionItem : ModuleOptionItem
{
    public required IEnumerable<string> AllowedValues { get; init; }
}

public class ToggleModuleOptionItem : ModuleOptionItem
{
    public bool IsChecked
    {
        get => Value == bool.TrueString;
        set => Value = value.ToString();
    }
}

public class ModuleOptionBuilder
{
    private IEnumerable<string> _allowedValues = [];
    private string? _description;
    private string? _displayName;
    private string _name = "";
    private string _value = "";
    private Type? _valueType;

    public ModuleOptionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ModuleOptionBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public ModuleOptionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ModuleOptionBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public ModuleOptionBuilder WithValue<T>(T value)
    {
        _value = value?.ToString() ?? "";
        return this;
    }

    public ModuleOptionBuilder WithNameAndValue<T>(T value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
    {
        _value = value?.ToString() ?? "";
        _name = valueExpression.Split('.').LastOrDefault() ?? "";
        _displayName = _name;
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues(IEnumerable<string> allowedValues)
    {
        _allowedValues = allowedValues;
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues<T>(IEnumerable<T> allowedValues)
    {
        _allowedValues = allowedValues.Where(x => x is not null).Select(x => x!.ToString()!);
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues<T>()
        where T : struct, Enum
    {
        _allowedValues = Enum.GetNames<T>();
        return this;
    }

    public bool HasAllowedValues()
    {
        return _allowedValues.Any();
    }

    public ModuleOptionItem ToPluginOption()
    {
        return new ModuleOptionItem
        {
            Name = _name,
            DisplayName = _displayName,
            Description = _description,
            Value = _value
        };
    }

    public SelectableModuleOptionItem ToSelectablePluginOption()
    {
        return new SelectableModuleOptionItem
        {
            Name = _name,
            DisplayName = _displayName,
            Description = _description,
            Value = _value,
            AllowedValues = _allowedValues
        };
    }

    public ToggleModuleOptionItem ToTogglePluginOption()
    {
        return new ToggleModuleOptionItem()
        {
            Name = _name,
            DisplayName = _displayName,
            Description = _description,
            Value = _value,
        };
    }
}

[JsonConverter(typeof(ModuleOptionsConverter))]
public class ModuleOptions(IEnumerable<ModuleOptionItem> items) : List<ModuleOptionItem>(items.ToList())
{
    public ModuleOptions() : this([]) { }

    public ModuleOptions AddOption(Func<ModuleOptionBuilder, ModuleOptionItem> creator)
    {
        var builder = new ModuleOptionBuilder();
        Add(creator(builder));
        return this;
    }

    public bool TrySetValue(string name, string value)
    {
        if (this.FirstOrDefault(x => x.Name == name) is not { } option)
        {
            return false;
        }

        option.Value = value;
        return true;
    }
}

public abstract class OverridableConfig
{
    public void UpdateValues(List<ModuleOptionItem> options)
    {
        var type = GetType();
        foreach (var option in options)
        {
            var propInfo = type.GetProperty(option.Name);
            var currentValue = propInfo!.GetValue(this);
            var optionValue = GetValue(option, option.Name, propInfo.PropertyType, currentValue);
            if (optionValue is not null)
            {
                propInfo.SetValue(this, optionValue);
            }
        }
    }

    public ModuleOptions ToModuleOptions()
    {
        var options = new ModuleOptions();
        foreach (var propertyInfo in GetType().GetProperties())
        {
            if (propertyInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
            {
                continue;
            }

            var builder = new ModuleOptionBuilder()
                          .WithName(propertyInfo.Name)
                          .WithDisplayName(propertyInfo.Name)
                          .WithValue(propertyInfo.GetValue(this));

            if (propertyInfo.PropertyType.IsEnum)
            {
                builder.WithAllowedValues(Enum.GetNames(propertyInfo.PropertyType));
            }

            if (propertyInfo.GetCustomAttribute<DescriptionAttribute>() is { } descriptionAttribute)
            {
                builder.WithDescription(descriptionAttribute.Description);
            }

            if (propertyInfo.GetCustomAttribute<DisplayNameAttribute>() is { } displayNameAttribute)
            {
                builder.WithDisplayName(displayNameAttribute.DisplayName);
            }

            if (propertyInfo.GetCustomAttribute<AllowedValuesAttribute>() is { } allowedValuesAttribute)
            {
                builder.WithAllowedValues(allowedValuesAttribute.Values);
            }

            ModuleOptionItem option;
            if (propertyInfo.PropertyType == typeof(bool))
            {
                option = builder.ToTogglePluginOption();
            }
            else if (builder.HasAllowedValues())
            {
                option = builder.ToSelectablePluginOption();
            }
            else
            {
                option = builder.ToPluginOption();
            }
            
            options.Add(option);
        }

        return options;
    }

    protected virtual object? GetValue(ModuleOptionItem options, string name, Type t, object? defaultValue)
    {
        if (t == typeof(int))
        {
            return options.GetInt32(name, (int)defaultValue!);
        }

        if (t == typeof(double))
        {
            return options.GetDouble(name, (double)defaultValue!);
        }

        if (t == typeof(string))
        {
            return options.GetString(name, (string)defaultValue!);
        }

        if (t == typeof(bool))
        {
            return options.GetBool(name, (bool)defaultValue!);
        }
        
        return t.IsEnum ? options.GetEnum(t, name, defaultValue!) : null;
    }
}

public class ModuleOptionsConverter : JsonConverter<ModuleOptions>
{
    public override ModuleOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = JsonSerializer.Deserialize<List<ModuleOptionItem>>(ref reader, options);
        return items is null ? null : new ModuleOptions(items);
    }

    public override void Write(Utf8JsonWriter writer, ModuleOptions value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<IEnumerable<ModuleOptionItem>>(writer, value, options);
    }
}