namespace TotoroNext.Torrents.Abstractions;

public class AddTorrentRequest
{
    public required string[] Torrents { get; init; }
    public required string SaveDirectory { get; init; }
    public string? Tags { get; init; } = "totoro";
}