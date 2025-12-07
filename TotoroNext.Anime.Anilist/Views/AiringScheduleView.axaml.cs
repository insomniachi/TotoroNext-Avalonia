using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace TotoroNext.Anime.Anilist.Views;

public partial class AiringScheduleView : UserControl
{
    public AiringScheduleView()
    {
        InitializeComponent();
    }
}

public class DayMatchConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DayOfWeek today || parameter is not DayOfWeek day)
        {
            return BindingOperations.DoNothing;
        }

        return today == day;
    }


    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}