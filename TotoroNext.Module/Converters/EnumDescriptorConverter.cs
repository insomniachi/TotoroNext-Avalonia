using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace TotoroNext.Module.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue)
        {
            return $"{value}";
        }

        var field = enumValue.GetType().GetField(enumValue.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? enumValue.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b || parameter is not { } enumValue)
        {
            return BindingOperations.DoNothing;
        }

        return Enum.Parse(targetType, enumValue.ToString() ?? "");
    }
}