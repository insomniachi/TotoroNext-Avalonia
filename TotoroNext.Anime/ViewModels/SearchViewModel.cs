using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;


public partial class SearchViewModel(IFactory<IMetadataService, Guid> factory,
									 IFactory<IAnimeProvider, Guid> providerFactory,
									 IMessenger messenger) : ObservableObject, IInitializable
{
	private readonly IMetadataService? _metadataService = factory.CreateDefault();
	private readonly IAnimeProvider? _provider = providerFactory.CreateDefault();
	
	[ObservableProperty] public partial string Query { get; set; } = "";

	[ObservableProperty] public partial List<AnimeModel> Items { get; set; } = [];

	public void Initialize()
	{
		this.WhenAnyValue(x => x.Query)
			.Where(_ => _metadataService is not null)
			.Where(query => query is { Length: > 3 })
			.Throttle(TimeSpan.FromMilliseconds(500))
			.SelectMany(_metadataService!.SearchAnimeAsync)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(items => Items = items);
	}

	public async Task WatchAnime(AnimeModel model)
	{
		if (_provider is null)
		{
			return;
		}

		if (await _provider.SearchAndSelectAsync(model) is not { } result)
		{
			return;
		}

		messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(result, model)));
	}
}
