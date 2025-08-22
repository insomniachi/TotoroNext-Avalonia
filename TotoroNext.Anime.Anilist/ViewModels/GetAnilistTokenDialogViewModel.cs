using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Anime.Anilist.ViewModels;

public partial class GetAnilistTokenDialogViewModel : ObservableObject
{
    [ObservableProperty] public partial string Token { get; set; } = "";
}