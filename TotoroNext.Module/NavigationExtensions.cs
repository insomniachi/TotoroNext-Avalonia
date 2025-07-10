using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class NavigationExtensions
{
    static NavigationExtensions()
    {
        IsAttachedProperty.Changed.AddClassHandler<TransitioningContentControl>(OnIsAttachedChanged);
    }
    
    public static readonly AttachedProperty<bool> IsAttachedProperty =
        AvaloniaProperty.RegisterAttached<NavigationExtensions, TransitioningContentControl, bool>(
            "IsAttached",
            defaultValue: false);

    public static void SetIsAttached(AvaloniaObject element, bool value) =>
        element.SetValue(IsAttachedProperty, value);

    public static bool GetIsAttached(AvaloniaObject element) =>
        element.GetValue(IsAttachedProperty);

    private static void OnIsAttachedChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not TransitioningContentControl control || args.NewValue is not true)
        {
            return;
        }

        var navigator = ActivatorUtilities.CreateInstance<NavigatorHost>(Container.Services, control);
        TrySetNavigator(control, navigator);

        control.DataContextChanged += (_, _) =>
        {
            TrySetNavigator(control, navigator);
        };
        return;

        void TrySetNavigator(TransitioningContentControl tcc, INavigator n)
        {
            if (tcc.DataContext is INavigatorHost nh)
            {
                nh.Navigator = n;
            }
        }
    }
}
