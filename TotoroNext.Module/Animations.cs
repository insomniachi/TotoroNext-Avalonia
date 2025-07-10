using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace TotoroNext.Module;

public class Animations
{
    private static ImplicitAnimationCollection? _implicitAnimationCollection;

    public static readonly AttachedProperty<TimeSpan> ItemsReorderAnimationDurationProperty =
        AvaloniaProperty.RegisterAttached<Animations, ItemsControl, TimeSpan>(
                                                                              "ItemsReorderAnimationDuration",
                                                                              TimeSpan.Zero);

    static Animations()
    {
        ItemsReorderAnimationDurationProperty.Changed.AddClassHandler<ItemsControl>(
             OnItemsReorderAnimationDurationChanged);
    }

    private static void OnItemsReorderAnimationDurationChanged(ItemsControl sender,
                                                               AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not TimeSpan duration)
        {
            return;
        }

        if (duration == TimeSpan.Zero)
        {
            return;
        }

        sender.ContainerPrepared += (_, ea) =>
        {
            if (ElementComposition.GetElementVisual(ea.Container) is not { } compositionVisual)
            {
                return;
            }

            var compositor = compositionVisual.Compositor;
            compositionVisual.ImplicitAnimations = GetOrCreateAnimation(compositor, duration);
        };
    }

    public static TimeSpan GetItemsReorderAnimationDuration(ItemsControl control)
    {
        return control.GetValue(ItemsReorderAnimationDurationProperty);
    }

    public static void SetItemsReorderAnimationDuration(ItemsControl control, TimeSpan value)
    {
        control.SetValue(ItemsReorderAnimationDurationProperty, value);
    }

    private static ImplicitAnimationCollection GetOrCreateAnimation(Compositor compositor, TimeSpan duration)
    {
        if (_implicitAnimationCollection is not null)
        {
            return _implicitAnimationCollection;
        }

        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        offsetAnimation.Duration = duration;
        _implicitAnimationCollection = compositor.CreateImplicitAnimationCollection();
        _implicitAnimationCollection["Offset"] = offsetAnimation;

        return _implicitAnimationCollection;
    }
}