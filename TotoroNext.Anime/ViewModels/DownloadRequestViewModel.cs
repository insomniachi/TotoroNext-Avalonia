using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class DownloadRequestViewModel(
    AnimeModel anime,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IMessenger messenger,
    IEnumerable<Descriptor> descriptors) : ObservableObject, IInitializable, IDialogViewModel
{
    private IAnimeProvider? _provider;

    [ObservableProperty] public partial Guid? ProviderId { get; set; }
    [ObservableProperty] public partial SearchResult? SelectedResult { get; set; }
    [ObservableProperty] public partial List<SearchResult> ProviderResults { get; set; } = [];
    [ObservableProperty] public partial string? SearchTerm { get; set; }
    [ObservableProperty] public partial bool CanDownload { get; set; }
    [ObservableProperty] public partial int Start { get; set; } = 1;
    [ObservableProperty] public partial int End { get; set; }
    [ObservableProperty] public partial string? SaveFolder { get; set; }
    [ObservableProperty] public partial string? FilenameFormat { get; set; }
    [ObservableProperty] public partial int EpisodeOffset { get; set; }

    public List<Descriptor> Providers { get; } =
        [..descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider) && x.Components.Contains(ComponentTypes.AnimeDownloader))];


    public Task Handle(DialogResult result)
    {
        if (result != DialogResult.OK || SelectedResult is null || _provider is null)
        {
            return Task.CompletedTask;
        }

        messenger.Send(new DownloadRequest
        {
            Anime = anime,
            Provider = _provider,
            SearchResult = SelectedResult,
            EpisodeStart = Start,
            EpisodeEnd = End,
            SaveFolder = SaveFolder,
            FilenameFormat = FilenameFormat,
            EpisodeOffset = EpisodeOffset
        });

        return Task.CompletedTask;
    }

    public void Initialize()
    {
        ProviderId = Providers.FirstOrDefault()?.Id;
        SearchTerm = anime.Title;
        End = anime.TotalEpisodes ?? 0;

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .Where(x => x != Guid.Empty)
            .Select(id =>
            {
                _provider = providerFactory.Create(id!.Value);
                return Observable.FromAsync(ct => _provider.GetSearchResults(SearchTerm, ct));
            })
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                var currentResult = SelectedResult;
                ProviderResults = results;
                SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == currentResult?.Title) ??
                                 ProviderResults.FirstOrDefault(x => x.Title == SearchTerm);
            });

        this.WhenAnyValue(x => x.SearchTerm)
            .Skip(1)
            .Where(x => x is { Length: > 2 })
            .Where(_ => ProviderId.HasValue)
            .Select(term =>
            {
                var provider = providerFactory.Create(ProviderId!.Value);
                return Observable.FromAsync(ct => provider.GetSearchResults(term, ct));
            })
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(results =>
            {
                var currentResult = SelectedResult;
                ProviderResults = results;
                SelectedResult = ProviderResults.FirstOrDefault(x => x.Title == currentResult?.Title) ??
                                 ProviderResults.FirstOrDefault(x => x.Title == SearchTerm);
            });

        this.WhenAnyValue(x => x.SelectedResult, x => x.Start, x => x.End)
            .Select(x => x.Item1 is not null &&
                         x is { Item2: > 0, Item3: > 0 } &&
                         x.Item3 >= x.Item2 &&
                         _provider is not null)
            .Subscribe(canDownload => CanDownload = canDownload);
    }
}