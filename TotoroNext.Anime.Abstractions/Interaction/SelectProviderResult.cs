using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Interaction;

[UsedImplicitly]
public sealed class SelectProviderResult : SelectResult<SearchResult>
{
    protected override Control CreateElement(SearchResult model)
    {
        return new Grid { ColumnSpacing = 8 }
               .Margin(8)
               .Cols("Auto,*")
               .Children(CreateImage(model?.Image?.ToString()), new TextBlock()
                                                                .Text(model?.Title ?? "")
                                                                .VerticalAlignment(VerticalAlignment.Center)
                                                                .TextWrapping(TextWrapping.Wrap)
                                                                .Col(1));
    }
}

[UsedImplicitly]
public sealed class SelectAnimeResult : SelectResult<Models.AnimeModel>
{
    protected override Control CreateElement(Models.AnimeModel model)
    {
        return new Grid { ColumnSpacing = 8 }
               .Margin(8)
               .Cols("Auto,*")
               .Children(CreateImage(model.Image), new TextBlock()
                                                   .Text(model.Title)
                                                   .VerticalAlignment(VerticalAlignment.Center)
                                                   .TextWrapping(TextWrapping.Wrap)
                                                   .Col(1));
    }
}