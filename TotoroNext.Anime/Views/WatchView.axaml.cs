using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LibVLCSharp.Avalonia;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Models;
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

        return new VideoView
        {
            [Grid.RowProperty] = 0,
            [Grid.ColumnProperty] = 0,
            MediaPlayer = embeddedVlcPlayer.MediaPlayer,
            Content = new TransportControls
            {
                [DataContextProperty] = vm
            }
        };
    }
}

public class EpisodeTemplateSelector : IDataTemplate
{
    public IDataTemplate? MinimalTemplate { get; set; }
    public IDataTemplate? DetailedTemplate { get; set; }

    // Build the DataTemplate here
    public Control? Build(object? param)
    {
        return param switch
        {
            Episode episode => string.IsNullOrEmpty(episode.Info?.Titles.English) ? MinimalTemplate?.Build(param) : DetailedTemplate?.Build(param),
            EpisodeInfo epInfo => string.IsNullOrEmpty(epInfo.Titles.English) ? MinimalTemplate?.Build(param) : DetailedTemplate?.Build(param),
            _ => new TextBlock { Text = "Invalid Data" }
        };
    }

    // Check if we can accept the provided data
    public bool Match(object? data)
    {
        return data is Episode or EpisodeInfo;
    }
}