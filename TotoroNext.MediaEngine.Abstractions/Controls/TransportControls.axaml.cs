using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using IconPacks.Avalonia.MaterialDesign;
using ReactiveUI;

namespace TotoroNext.MediaEngine.Abstractions.Controls;

public partial class TransportControls : UserControl
{
    public TransportControls()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<EnterFullScreen>(this, (_, _) => FullScreenIcon.Kind = PackIconMaterialDesignKind.FullscreenExit);
        WeakReferenceMessenger.Default.Register<ExitFullScreen>(this, (_, _) => FullScreenIcon.Kind = PackIconMaterialDesignKind.Fullscreen);

        PositionSlider.AddHandler(PointerPressedEvent,
                                  OnSliderPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

        PositionSlider.AddHandler(PointerReleasedEvent,
                                  OnSliderReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

        var interaction = Observable.FromEventPattern<PointerEventArgs>(
                                                                        h => PointerMoved += h,
                                                                        h => PointerMoved -= h);

        interaction
            .Throttle(TimeSpan.FromSeconds(3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                Cursor = new Cursor(StandardCursorType.None);
                MediaControlsGrid.Opacity = 0;
            });

        interaction
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                Cursor = Cursor.Default;
                MediaControlsGrid.Opacity = 1;
            });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var gesture = new KeyGesture(e.Key, e.KeyModifiers);
        WeakReferenceMessenger.Default.Send(gesture);
        base.OnKeyDown(e);
    }

    private void OnSliderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not TransportControlsViewModel vm)
        {
            return;
        }

        vm.IsSeeking = true;
    }

    private void OnSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not TransportControlsViewModel vm)
        {
            return;
        }

        vm.CompleteSeek();
        vm.IsSeeking = false;
    }
}