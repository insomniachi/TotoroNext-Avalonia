using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace TotoroNext.Module;

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

    public static T CreateFrom<T>(List<ModuleOptionItem> options)
        where T : class, new()
    {
        var type = typeof(T);
        return (T)CreateType(type, options);
    }
    
    public static object CreateType(Type type, List<ModuleOptionItem> options)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var option in options)
        {
            var propInfo = type.GetProperty(option.Name);
            var currentValue = propInfo!.GetValue(instance);
            var optionValue = GetValue(option, option.Name, propInfo.PropertyType, currentValue);
            if (optionValue is not null)
            {
                propInfo.SetValue(instance, optionValue);
            }
        }
        return instance;
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

            if (propertyInfo.PropertyType == typeof(bool))
            {
                builder.WithEditorType(SpecialEditorType.ToggleSwitch);
            }
            else if (builder.HasAllowedValues())
            {
                builder.WithEditorType(SpecialEditorType.ComboBox);
            }

            if (propertyInfo.GetCustomAttribute<SpecialEditorTypeAttribute>() is { } editorTypeAttribute)
            {
                builder.WithEditorType(editorTypeAttribute.Type);
            }

            ConfigureProperty(builder, propertyInfo);

            options.Add(builder.ToPluginOption());
        }

        return options;
    }

    protected virtual void ConfigureProperty(ModuleOptionBuilder builder, PropertyInfo info) { }

    private static object? GetValue(ModuleOptionItem options, string name, Type t, object? defaultValue)
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