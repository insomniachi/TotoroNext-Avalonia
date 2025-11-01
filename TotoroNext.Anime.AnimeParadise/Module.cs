﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeParadise;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("3b61e01b-dd7c-4492-a564-4ee031959097"),
        Name = "Anime Paradise",
        HeroImage = ResourceHelper.GetResource("animeparadise.jpg"),
        Description = "Watch anime, always up to date and in high quality, with multiple sub direct from Japan The God of High School, Attack on Titan and more!",
        Components = [ComponentTypes.AnimeProvider]
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}

public class Settings : OverridableConfig
{
    [DisplayName("Subtitle")]
    [AllowedValues("English")]
    public string SubtitleLanguage { get; set; } = "English";
}