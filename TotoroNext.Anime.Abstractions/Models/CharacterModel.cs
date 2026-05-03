namespace TotoroNext.Anime.Abstractions.Models;

public class CharacterModel
{
    public string Name { get; set; } = "";
    public Uri? Image { get; set; }
    public string Description { get; set; } = "";
    public string Gender { get; set; } = "";
}