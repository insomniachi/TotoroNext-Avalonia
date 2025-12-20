using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
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

        menu.Tapped += (_, _) => { WeakReferenceMessenger.Default.Send(new NavigateToViewModelMessage(type)); };
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

    public static void ConfigureView(StyledElement view, object vm)
    {
        view.DataContext = vm;
        view.AttachedToLogicalTree += (_, _) =>
        {
            HandleClosable(view, vm);
            HandleInitializable(vm);
            HandleKeyBindings(vm, false);
            _ = HandleIAsyncInitializable(vm);
        };
        
        view.DetachedFromLogicalTree += (_, _) =>
        {
            HandleDisposable(vm);
            HandleKeyBindings(vm, true);
            _ = HandleAsyncDisposable(vm);
        };
    }

    private static void HandleKeyBindings(object vm, bool isDetaching)
    {
        if (vm is not IKeyBindingsProvider kbp)
        {
            return;
        }

        var scope = Container.Services.GetRequiredService<IKeyBindingsManager>();
        if (isDetaching)
        {
            scope.RemoveProvider(kbp);
        }
        else
        {
            scope.AddProvider(kbp);
        }
    }

    private static void HandleClosable(StyledElement view, object vm)
    {
        if (vm is not ICloseable closeable)
        {
            return;
        }

        closeable.Closed += (_, _) =>
        {
            if (FindLogicalParentOfType<DefaultDrawerControl>(view) is not { } drawer)
            {
                return;
            }

            drawer.Close();
        };
    }

    private static void HandleInitializable(object vm)
    {
        if (vm is not IInitializable i)
        {
            return;
        }

        i.Initialize();
    }

    private static async Task HandleIAsyncInitializable(object vm)
    {
        if (vm is not IAsyncInitializable ia)
        {
            return;
        }

        try
        {
            await ia.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static void HandleDisposable(object vm)
    {
        if (vm is not IDisposable disposable)
        {
            return;
        }

        disposable.Dispose();
    }

    private static async Task HandleAsyncDisposable(object vm)
    {
        if (vm is not IAsyncDisposable asyncDisposable)
        {
            return;
        }

        await asyncDisposable.DisposeAsync();
    }

    private static T? FindLogicalParentOfType<T>(ILogical? control) where T : class
    {
        while (control != null)
        {
            control = control.LogicalParent;
            if (control is T typedParent)
            {
                return typedParent;
            }
        }

        return null;
    }
}