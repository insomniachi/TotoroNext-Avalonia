using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace TotoroNext.Module;

public abstract class OverridableConfig
{
    public void UpdateValues(List<DataContainerProperty> options)
    {
        var type = GetType();
        foreach (var option in options)
        {
            var propInfo = type.GetProperty(option.Name);
            var currentValue = propInfo!.GetValue(this);
            var optionValue = GetValue(option, propInfo.PropertyType, currentValue);
            if (optionValue is not null)
            {
                propInfo.SetValue(this, optionValue);
            }
        }
    }

    public static T CreateFrom<T>(List<DataContainerProperty> options)
        where T : class, new()
    {
        var type = typeof(T);
        return (T)CreateType(type, options);
    }
    
    public static object CreateType(Type type, List<DataContainerProperty> options)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var option in options)
        {
            var propInfo = type.GetProperty(option.Name);
            var currentValue = propInfo!.GetValue(instance);
            var optionValue = GetValue(option, propInfo.PropertyType, currentValue);
            if (optionValue is not null)
            {
                propInfo.SetValue(instance, optionValue);
            }
        }
        return instance;
    }

    public DataContainer ToModuleOptions()
    {
        var options = new DataContainer();
        foreach (var propertyInfo in GetType().GetProperties())
        {
            if (propertyInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
            {
                continue;
            }

            var builder = new DataContainerBuilder()
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

            options.Add(builder.ToProperty());
        }

        return options;
    }

    protected virtual void ConfigureProperty(DataContainerBuilder builder, PropertyInfo info) { }

    private static object? GetValue(DataContainerProperty options, Type t, object? defaultValue)
    {
        if (t == typeof(int))
        {
            return options.GetInt32((int)defaultValue!);
        }

        if (t == typeof(double))
        {
            return options.GetDouble((double)defaultValue!);
        }

        if (t == typeof(string))
        {
            return options.GetString((string)defaultValue!);
        }

        if (t == typeof(bool))
        {
            return options.GetBool((bool)defaultValue!);
        }

        return t.IsEnum ? options.GetAsType(t, defaultValue) : null;
    }
}