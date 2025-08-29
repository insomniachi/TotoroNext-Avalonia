using IconPacks.Avalonia.MaterialDesign;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.SongRecognition.ViewModels;
using TotoroNext.SongRecognition.Views;

namespace TotoroNext.SongRecognition;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("d98f3601-7c85-4c46-b01a-083133fb9364"),
        Name = "Song Recognition",
        Description = "Recognize songs played with shazam",
        HeroImage = ResourceHelper.GetResource("shazam.jpg")
    };


    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddChildNavigationViewItem<SongRecognitionView, SongRecognitionViewModel>("AniGuesser", "Song Recognition",
                                                                                           PackIconMaterialDesignKind.MusicNote);
    }
}