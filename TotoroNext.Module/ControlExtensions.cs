﻿using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TotoroNext.Module;

public class ControlExtensions
{
    public static readonly AttachedProperty<ICommand> RightTappedCommandProperty =
        AvaloniaProperty.RegisterAttached<ControlExtensions, Control, ICommand>(@"RightTappedCommand");

    public static readonly AttachedProperty<ICommand> TappedCommandProperty =
        AvaloniaProperty.RegisterAttached<ControlExtensions, Control, ICommand>("TappedCommand");

    static ControlExtensions()
    {
        RightTappedCommandProperty.Changed.AddClassHandler<Control>(OnRightTappedCommandAdded);
        TappedCommandProperty.Changed.AddClassHandler<Control>(OnTappedCommandAdded);
    }

    private static void OnTappedCommandAdded(Control sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not ICommand)
        {
            return;
        }

        sender.AddHandler(Gestures.TappedEvent, OnTapped);
    }

    private static void OnRightTappedCommandAdded(Control sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not ICommand command)
        {
            return;
        }

        sender.AddHandler(Gestures.RightTappedEvent, OnRightTapped);
    }

    private static void OnRightTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control c)
        {
            return;
        }

        var command = GetRightTappedCommand(c);

        if (c.DataContext is { } dataContext)
        {
            command.Execute(dataContext);
        }
        else
        {
            command.Execute(null);
        }
    }
    
    private static void OnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control c)
        {
            return;
        }

        var command = GetTappedCommand(c);

        if (c.DataContext is { } dataContext)
        {
            command.Execute(dataContext);
        }
        else
        {
            command.Execute(null);
        }
    }

    public static void SetRightTappedCommand(AvaloniaObject element, ICommand command)
    {
        element.SetValue(RightTappedCommandProperty, command);
    }

    public static ICommand GetRightTappedCommand(AvaloniaObject element)
    {
        return element.GetValue(RightTappedCommandProperty);
    }

    public static void SetTappedCommand(AvaloniaObject element, ICommand command)
    {
        element.SetValue(TappedCommandProperty, command);
    }

    public static ICommand GetTappedCommand(AvaloniaObject element)
    {
        return element.GetValue(TappedCommandProperty);
    }
}