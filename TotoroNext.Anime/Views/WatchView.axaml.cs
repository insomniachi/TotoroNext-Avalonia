using System.Globalization;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using LibVLCSharp.Avalonia;
using ReactiveUI;
using TotoroNext.Anime.ViewModels;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.MediaEngine.Abstractions.Controls;

namespace TotoroNext.Anime.Views;

public partial class WatchView : UserControl
{
    public WatchView()
    {
        InitializeComponent();
        this.GetObservable(DataContextProperty)
            .Select(x => x as WatchViewModel)
            .WhereNotNull()
            .Where(x => x.MediaPlayer is IEmbeddedVlcMediaPlayer)
            .Select(vm => CreateEmbeddedVideoView(vm.MediaPlayer))
            .WhereNotNull()
            .Subscribe(view => MainGrid.Children.Add(view));
    }

    private static VideoView? CreateEmbeddedVideoView(IMediaPlayer? mediaPlayer)
    {
        if (mediaPlayer is not IEmbeddedVlcMediaPlayer embeddedVlcPlayer)
        {
            return null;
        }

        var vm = new TransportControlsViewModel(embeddedVlcPlayer);
        vm.Initialize();
        
        return new VideoView()
        {
            [Grid.RowProperty] = 0,
            [Grid.ColumnProperty] = 0,
            MediaPlayer = embeddedVlcPlayer.MediaPlayer,
            Content = new TransportControls()
            {
                [DataContextProperty] = vm
            }
        };
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb)
        {
            return;
        }

        if (e.AddedItems is not { Count: 1 })
        {
            return;
        }

        if (e.AddedItems[0] is not { } item)
        {
            return;
        }

        lb.ScrollIntoView(item);
    }
}