using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Converters;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace TotoroNext.Module.Controls;

/// <remarks>
///     Reference: https://github.com/AvaloniaCommunity/Material.Avalonia/blob/master/Material.Ripple/RippleHandler.cs
/// </remarks>
internal class RippleHandler : CompositionCustomVisualHandler
{
    public static readonly object FirstStepMessage = new(), SecondStepMessage = new();

    private readonly IImmutableBrush _brush;
    private readonly Point _center;
    private readonly RoundedRect _cornerRadiusRect;
    private readonly TimeSpan _duration;
    private readonly Easing _easing;

    private readonly double _maxRadius;
    private readonly double _opacity;
    private readonly bool _transitions;
    private TimeSpan _animationElapsed;
    private TimeSpan? _lastServerTime;
    private TimeSpan? _secondStepStart;

    public RippleHandler(IImmutableBrush brush,
                         Easing easing,
                         TimeSpan duration,
                         double opacity,
                         CornerRadius cornerRadius,
                         double positionX, double positionY,
                         double outerWidth, double outerHeight, bool transitions)
    {
        _brush = brush;
        _easing = easing;
        _duration = duration;
        _opacity = opacity;
        _cornerRadiusRect = new RoundedRect(new Rect(0, 0, outerWidth, outerHeight),
                                            cornerRadius.BottomLeft, cornerRadius.BottomRight,
                                            cornerRadius.BottomRight, cornerRadius.BottomLeft);
        _transitions = transitions;
        _center = new Point(positionX, positionY);

        _maxRadius = Math.Sqrt(Math.Pow(outerWidth, 2) + Math.Pow(outerHeight, 2));
    }

    public override void OnRender(ImmediateDrawingContext drawingContext)
    {
        if (_lastServerTime.HasValue)
        {
            _animationElapsed += CompositionNow - _lastServerTime.Value;
        }

        _lastServerTime = CompositionNow;

        var currentRadius = _maxRadius;
        var currentOpacity = _opacity;

        if (_transitions)
        {
            var expandingStep = _easing.Ease((double)_animationElapsed.Ticks / _duration.Ticks);
            currentRadius = _maxRadius * expandingStep;

            if (_secondStepStart is { } secondStepStart)
            {
                var opacityStep = _easing.Ease((double)(_animationElapsed - secondStepStart).Ticks /
                                               (_duration - secondStepStart).Ticks);
                currentOpacity = _opacity - _opacity * opacityStep;
            }
        }

        using (drawingContext.PushClip(_cornerRadiusRect))
        {
            using (drawingContext.PushOpacity(currentOpacity, default))
            {
                drawingContext.DrawEllipse(_brush, null, _center, currentRadius, currentRadius);
            }
        }
    }

    public override void OnMessage(object message)
    {
        if (message == FirstStepMessage)
        {
            _lastServerTime = null;
            _secondStepStart = null;
            RegisterForNextAnimationFrameUpdate();
        }
        else if (message == SecondStepMessage)
        {
            _secondStepStart = _animationElapsed;
        }
    }

    public override void OnAnimationFrameUpdate()
    {
        if (_animationElapsed >= _duration)
        {
            return;
        }

        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }
}

/// <summary>
///     Provides static properties for configuring ripple effects.
/// </summary>
/// <remarks>
///     Reference: https://github.com/AvaloniaCommunity/Material.Avalonia/blob/master/Material.Ripple/Ripple.cs
/// </remarks>
public static class Ripple
{
    /// <summary>
    ///     Gets or sets the easing function used for ripple animations.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="CircularEaseOut" />.
    /// </remarks>
    public static Easing Easing { get; set; } = new CircularEaseOut();

    /// <summary>
    ///     Gets or sets the duration of ripple animations.
    /// </summary>
    /// <remarks>
    ///     Defaults to 800 milliseconds.
    /// </remarks>
    public static TimeSpan Duration { get; set; } = new(0, 0, 0, 0, 800);
}

public static class CornerRadiusFilterConverters
{
    /// <summary>
    /// A <see cref="CornerRadiusFilterConverter" /> that filters the top corners (TopLeft and TopRight).
    /// </summary>
    public static readonly CornerRadiusFilterConverter Top =
        new() { Filter = Corners.TopLeft | Corners.TopRight };

    /// <summary>
    /// A <see cref="CornerRadiusFilterConverter" /> that filters the right corners (TopRight and BottomRight).
    /// </summary>
    public static readonly CornerRadiusFilterConverter Right =
        new() { Filter = Corners.TopRight | Corners.BottomRight };

    /// <summary>
    /// A <see cref="CornerRadiusFilterConverter" /> that filters the bottom corners (BottomLeft and BottomRight).
    /// </summary>
    public static readonly CornerRadiusFilterConverter Bottom =
        new() { Filter = Corners.BottomLeft | Corners.BottomRight };

    /// <summary>
    /// A <see cref="CornerRadiusFilterConverter" /> that filters the left corners (TopLeft and BottomLeft).
    /// </summary>
    public static readonly CornerRadiusFilterConverter Left =
        new() { Filter = Corners.TopLeft | Corners.BottomLeft };

    /// <summary>
    /// A <see cref="CornerRadiusToDoubleConverter" /> that converts the TopLeft corner radius to a double value.
    /// </summary>
    public static readonly CornerRadiusToDoubleConverter TopLeft =
        new() { Corner = Corners.TopLeft };

    /// <summary>
    /// A <see cref="CornerRadiusToDoubleConverter" /> that converts the TopRight corner radius to a double value.
    /// </summary>
    public static readonly CornerRadiusToDoubleConverter TopRight =
        new() { Corner = Corners.TopRight };

    /// <summary>
    /// A <see cref="CornerRadiusToDoubleConverter" /> that converts the BottomLeft corner radius to a double value.
    /// </summary>
    public static readonly CornerRadiusToDoubleConverter BottomLeft =
        new() { Corner = Corners.BottomLeft };

    /// <summary>
    /// A <see cref="CornerRadiusToDoubleConverter" /> that converts the BottomRight corner radius to a double value.
    /// </summary>
    public static readonly CornerRadiusToDoubleConverter BottomRight =
        new() { Corner = Corners.BottomRight };
}