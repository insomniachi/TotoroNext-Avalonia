using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.SongRecognition.Capture_Helpers;

namespace TotoroNext.SongRecognition.ViewModels;

[UsedImplicitly]
public sealed partial class SongRecognitionViewModel : ObservableObject, IInitializable, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    public ObservableCollection<string> SongTitles { get; } = [];
    public ObservableCollection<AniDb.AniDbItem> AnimeTitles { get; } = [];

    [ObservableProperty] public partial AniDb.AniDbItem? SelectedSong { get; set; }
    [ObservableProperty] public partial List<AniDb.AniDbItem> SearchedSongs { get; set; } = [];
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial List<AniDb.AniDbItem> SearchedAnimeTitles { get; set; } = [];

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    public void Initialize()
    {
        this.WhenAnyValue(x => x.SearchText)
            .Where(x => x is { Length: > 2 })
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(AniDb.SearchSongs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(anime => SearchedSongs = anime.DistinctBy(x => x.Id).ToList());

        this.WhenAnyValue(x => x.SelectedSong)
            .WhereNotNull()
            .Select(x => x.Id)
            .Select(AniDb.FindAnimeBySongId)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(anime => SearchedAnimeTitles = anime.DistinctBy(x => x.Id).ToList());

        _ = Start();
    }

    [RelayCommand]
    private void Clear()
    {
        SongTitles.Clear();
        AnimeTitles.Clear();
        SearchedAnimeTitles = [];
        SearchedSongs = [];
        SearchText = "";
    }

    private async Task Start()
    {
        while (!_cts.IsCancellationRequested)
        {
            var startTime = DateTime.Now;

            try
            {
                using var captureHelper = CreateCaptureHelper();
                captureHelper.Start();

                var result = await CaptureAndTag.RunAsync(captureHelper);

                if (result is { Success: true, Title: not null })
                {
                    if (!SongTitles.Contains(result.Title, StringComparer.OrdinalIgnoreCase))
                    {
                        SongTitles.Add(result.Title);
                    }

                    _ = Task.Run(async () =>
                    {
                        await foreach (var anime in AniDb.FindAnimeFromSong(result.Title))
                        {
                            if (AnimeTitles.All(x => x.Id != anime.Id))
                            {
                                RxApp.MainThreadScheduler.Schedule(() => AnimeTitles.Add(anime));
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var nextStartTime = startTime + TimeSpan.FromSeconds(15);
            while (DateTime.Now < nextStartTime)
            {
                await Task.Delay(100);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private static WasapiCaptureHelper CreateCaptureHelper()
    {
        return new WasapiCaptureHelper();
    }
}