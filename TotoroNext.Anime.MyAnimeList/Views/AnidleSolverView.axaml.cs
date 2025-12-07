using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using IconPacks.Avalonia.MaterialDesign;
using TotoroNext.Anime.MyAnimeList.ViewModels;

namespace TotoroNext.Anime.MyAnimeList.Views;

public partial class AnidleSolverView : UserControl
{
    public AnidleSolverView()
    {
        InitializeComponent();
    }
}

public class AnidleCriteriaIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AnidleCriteria criteria)
        {
            return BindingOperations.DoNothing;
        }

        if (criteria.IsValid)
        {
            return PackIconMaterialDesignKind.None;
        }

        return criteria.Compare == ">"
            ? PackIconMaterialDesignKind.ArrowDownward
            : PackIconMaterialDesignKind.ArrowUpward;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class AnidleCriteriaForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AnidleCriteria criteria)
        {
            return BindingOperations.DoNothing;
        }

        return criteria.IsValid
            ? new SolidColorBrush(Colors.Green)
            : new SolidColorBrush(Colors.Red);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}