using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Extensions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;


public partial class SearchViewModel(IFactory<IMetadataService, Guid> factory,
									 IFactory<IAnimeProvider, Guid> providerFactory,
									 IAnimeOverridesRepository overridesRepository,
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

	[RelayCommand]
	private async Task NavigateToWatch(AnimeModel anime)
	{
		var overrides = overridesRepository.GetOverrides(anime.Id);

		var provider = overrides?.Provider is { } providerId
			? providerFactory.Create(providerId)
			: providerFactory.CreateDefault();
		
		var term = string.IsNullOrEmpty(overrides?.SelectedResult)
			? anime.Title
			: overrides.SelectedResult;

		var result = await provider.SearchAndSelectAsync(term);

		if (overrides is not null)
		{
			messenger.Send(overrides);
		}

		if (result is null)
		{
			return;
		}

		messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(result, anime)));
	}

	[RelayCommand]
	private void OpenAnimeDetails(AnimeModel anime)
	{
		messenger.Send(new PaneNavigateToDataMessage(anime, paneWidth: 750, title: anime.Title));
	}
}
