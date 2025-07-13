using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.ViewModels;

public partial class AnimeOverridesViewModel(
    OverridesViewModelNavigationParameters parameters,
    IAnimeOverridesRepository animeOverridesRepository,
    IEnumerable<Descriptor> descriptors) : ObservableObject, IInitializable
{
    [ObservableProperty] public partial bool IsNsfw { get; set; }

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    public List<Descriptor> Providers { get; } = [.. descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];

    public void Initialize()
    {
        var overrides = animeOverridesRepository.GetOverrides(parameters.Anime.Id);

        IsNsfw = overrides?.IsNsfw ?? false;
        ProviderId = overrides?.Provider;

        this.WhenAnyValue(x => x.IsNsfw, x => x.ProviderId)
            .Skip(1)
            .Select(x => new AnimeOverrides
            {
                IsNsfw = x.Item1,
                Provider = x.Item2
            })
            .Subscribe(@override => animeOverridesRepository.CreateOrUpdate(parameters.Anime.Id, @override));
    }
}