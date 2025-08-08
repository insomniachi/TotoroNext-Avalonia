using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class ProviderDebuggerViewModel(IEnumerable<Descriptor> descriptors,
                                       IFactory<IAnimeProvider, Guid> providerFactory) : ObservableObject, IInitializable
{
        private IAnimeProvider? _provider;
    
    public List<Descriptor> Descriptors { get; } =
        [..descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];
    
    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial string Query { get; set; } = "";

    [ObservableProperty] public partial List<SearchResult> Result { get; set; } = [];
    
    [ObservableProperty] public partial SearchResult? SelectedResult { get; set; }

    [ObservableProperty] public partial List<Episode> Episodes { get; set; } = [];
    
    [ObservableProperty] public partial Episode? SelectedEpisode { get; set; }

    [ObservableProperty] public partial List<VideoServer> Servers { get; set; } = [];

    [ObservableProperty] public partial VideoServer? SelectedServer { get; set; }
   
    public void Initialize()
    {
        ProviderId = Descriptors.FirstOrDefault()?.Id;

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .Subscribe(id => _provider = providerFactory.Create(id!.Value));

        this.WhenAnyValue(x => x.Query)
            .Where(_ => _provider != null)
            .Where(x => x is { Length: > 2 })
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(query => _provider!.SearchAsync(query).ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(response => Result = response);

        this.WhenAnyValue(x => x.SelectedResult)
            .WhereNotNull()
            .SelectMany(x => x.GetEpisodes().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ep => Episodes = ep);

        this.WhenAnyValue(x => x.SelectedEpisode)
            .WhereNotNull()
            .SelectMany(x => x.GetServersAsync().ToListAsync().AsTask())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(servers => Servers = servers);

        this.WhenAnyValue(x => x.SelectedServer)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(server => MessageBox.ShowAsync(server.Url.ToString(), server.Name));
    }
}