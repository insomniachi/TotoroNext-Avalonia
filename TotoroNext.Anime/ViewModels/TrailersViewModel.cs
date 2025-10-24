using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class TrailersViewModel(List<TrailerVideo> trailers,
                                       IFactory<IMediaPlayer, Guid> mediaPlayerFactory) : ObservableObject, IInitializable
{
    private readonly IMediaPlayer? _mediaPlayer = mediaPlayerFactory.CreateDefault();
    public List<TrailerVideo> Trailers { get; } = trailers;
    [ObservableProperty] public partial TrailerVideo? SelectedTrailer { get; set; }
    
    public void Initialize()
    {
        this.WhenAnyValue(x => x.SelectedTrailer)
            .WhereNotNull()
            .Select(trailer => (trailer, _mediaPlayer))
            .Where(tuple => tuple is { Item2: not null })
            .Subscribe(tuple =>
            {
                var (trailer, player) = tuple;
                var media = new Media(new Uri(trailer.Url), new MediaMetadata(""));
                player!.Play(media, TimeSpan.Zero);
            });
    }
}