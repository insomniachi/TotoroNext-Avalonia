using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class RelationsBuilderViewModel(RelationsBuilderViewModelNavigationParameters parameters,
                                               IAnimeRelations relations) : DialogViewModel, IInitializable, IDialogViewModel
{
    public ObservableCollection<AnimeModel> Anime { get; } = [];

    public void Initialize()
    {
        Anime.AddRange(parameters.Anime);
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp(AnimeModel? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Anime.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        Anime.RemoveAt(index);
        Anime.Insert(index - 1, item);
        
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private bool CanMoveUp(AnimeModel? item)
    {
        return item != null && Anime.IndexOf(item) > 0;
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown(AnimeModel? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Anime.IndexOf(item);
        if (index >= Anime.Count - 1)
        {
            return;
        }

        Anime.RemoveAt(index);
        Anime.Insert(index + 1, item);
        
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private bool CanMoveDown(AnimeModel? item)
    {
        return item != null && Anime.IndexOf(item) < Anime.Count - 1;
    }

    [RelayCommand]
    private void Delete(AnimeModel? item)
    {
        if (item != null)
        {
            Anime.Remove(item);
        }
    }

    public async Task Handle(DialogResult result)
    {
        if(result is not DialogResult.OK)
        {
            return;
        }

        var sb = new StringBuilder();
        var first = Anime[0];
        var counter = first.TotalEpisodes ?? 0;
        foreach (var anime in Anime.Skip(1))
        {
            var absoluteEndEp = counter + (anime.TotalEpisodes ?? 0);
            var relation = new AnimeRelation()
            {
                DestinationEpisodesRage = new EpisodeRange(1, anime.TotalEpisodes ?? 0),
                SourceEpisodesRage = new EpisodeRange(counter + 1, absoluteEndEp),
                DestinationIds = anime.ExternalIds,
                SourceIds = first.ExternalIds
            };

            if (!relations.Exists(relation))
            {
                sb.AppendLine($"# {first.Title} -> {anime.Title.Replace(first.Title, "~")}");
                sb.AppendLine(relation.ToString());
                relations.AddRelation(relation);
            }
            
            counter = absoluteEndEp;
        }

        sb.AppendLine();
        
        var localRelations = FileHelper.GetPath("relations-local.txt");
        await File.AppendAllTextAsync(localRelations, sb.ToString());
    }
}