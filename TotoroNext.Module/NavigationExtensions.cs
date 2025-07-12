using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Module;

public class NavigationExtensions
{
    public static readonly AttachedProperty<bool> IsAttachedProperty =
        AvaloniaProperty.RegisterAttached<NavigationExtensions, TransitioningContentControl, bool>(
             "IsAttached");

    public static readonly AttachedProperty<Type> NavigateToViewModelProperty =
        AvaloniaProperty.RegisterAttached<NavigationExtensions, NavMenuItem, Type>("NavigateToViewModel");

    static NavigationExtensions()
    {
        IsAttachedProperty.Changed.AddClassHandler<TransitioningContentControl>(OnIsAttachedChanged);
        NavigateToViewModelProperty.Changed.AddClassHandler<NavMenuItem>(OnViewModelChanged);
    }

    private static void OnViewModelChanged(NavMenuItem menu, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not Type { } type)
        {
            return;
        }

        menu.Tapped += (_, _) =>
        {
            WeakReferenceMessenger.Default.Send(new NavigateToViewModelMessage(type));
        };
    }

    public static void SetNavigateToViewModel(AvaloniaObject element, Type value)
    {
        element.SetValue(NavigateToViewModelProperty, value);
    }

    public static Type GetNavigateToViewModel(AvaloniaObject element)
    {
        return element.GetValue(NavigateToViewModelProperty);
    }

    public static void SetIsAttached(AvaloniaObject element, bool value)
    {
        element.SetValue(IsAttachedProperty, value);
    }

    public static bool GetIsAttached(AvaloniaObject element)
    {
        return element.GetValue(IsAttachedProperty);
    }

    private static void OnIsAttachedChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not TransitioningContentControl control || args.NewValue is not true)
        {
            return;
        }

        var navigator = ActivatorUtilities.CreateInstance<NavigatorHost>(Container.Services, control);
        TrySetNavigator(control, navigator);

        control.DataContextChanged += (_, _) => { TrySetNavigator(control, navigator); };
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