using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace TotoroNext.Module;

public class WrapAnimationBehavior
{
    private static ImplicitAnimationCollection? _implicitAnimationCollection;
    
    static WrapAnimationBehavior()
    {
        EnableWrapOffsetAnimationProperty.Changed.AddClassHandler<Control>(OnEnableChanged);
    }
    
    public static readonly AttachedProperty<bool> EnableWrapOffsetAnimationProperty =
        AvaloniaProperty.RegisterAttached<WrapAnimationBehavior, Control, bool>(
            "EnableWrapOffsetAnimation",
            defaultValue: false,
            inherits: false);

    public static bool GetEnableWrapOffsetAnimation(Control control) =>
        control.GetValue(EnableWrapOffsetAnimationProperty);

    public static void SetEnableWrapOffsetAnimation(Control control, bool value) =>
        control.SetValue(EnableWrapOffsetAnimationProperty, value);

    private static void OnEnableChanged(Control sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        sender.LayoutUpdated += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyOffsetAnimation(sender);
            }, DispatcherPriority.Background);
        };
    }

    private static void ApplyOffsetAnimation(Control control)
    {
        if (control.GetVisualParent() is not { } visual)
        {
            return;
        }
        
        if (ElementComposition.GetElementVisual(visual) is not { } compositionVisual)
        {
            return;
        }
        
        var compositor = compositionVisual.Compositor;
        compositionVisual.ImplicitAnimations = GetOrCreateAnimation(compositor);
    }

    private static ImplicitAnimationCollection GetOrCreateAnimation(Compositor compositor)
    {
        if (_implicitAnimationCollection is not null)
        {
            return _implicitAnimationCollection;
        }
        
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);
        _implicitAnimationCollection = compositor.CreateImplicitAnimationCollection();
        _implicitAnimationCollection["Offset"] = offsetAnimation;

        return _implicitAnimationCollection;
    }
    
}