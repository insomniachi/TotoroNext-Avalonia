using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.SongRecognition.ViewModels;
using TotoroNext.SongRecognition.Views;

namespace TotoroNext.SongRecognition;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new Descriptor
    {
        Id = new Guid("d98f3601-7c85-4c46-b01a-083133fb9364"),
        Name = "Song Recognition",
    };
    

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddMainNavigationItem<SongRecognitionView, SongRecognitionViewModel>("Song Recognition", IconPacks.Avalonia.MaterialDesign.PackIconMaterialDesignKind.MusicNote);
    }
}