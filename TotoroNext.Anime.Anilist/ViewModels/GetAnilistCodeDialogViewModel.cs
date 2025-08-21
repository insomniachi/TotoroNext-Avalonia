using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Anime.Anilist.ViewModels;

public partial class GetAnilistCodeDialogViewModel : ObservableObject
{
    [ObservableProperty] public partial string Code { get; set; } = "";
}