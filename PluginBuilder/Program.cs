using PluginBuilder;

var plugins = new List<PluginProject>
{
    new("TotoroNext.Anime.AllAnime")
    {
        Dependencies =
        [
            "FlurlGraphQL",
            "Macross.Json.Extensions"
        ]
    },
    new("TotoroNext.Anime.AnimePahe"),
    new("TotoroNext.Anime.AnimeOnsen"),
    new("TotoroNext.Anime.AnimeParadise"),
    new("TotoroNext.Anime.Anizone"),
    new("TotoroNext.Anime.SubsPlease"),
    new("TotoroNext.Anime.Jellyfin")
    {
        Dependencies =
        [
            "Jellyfin.Sdk",
            "Microsoft.Kiota.Abstractions",
            "Microsoft.Kiota.Http.HttpClientLibrary",
            "Microsoft.Kiota.Serialization.Form",
            "Microsoft.Kiota.Serialization.Json",
            "Microsoft.Kiota.Serialization.Text",
            "Microsoft.Kiota.Serialization.Multipart",
            "Std.UriTemplate"
        ]
    },
    new("TotoroNext.Anime.AnimeGG"),
    new("TotoroNext.Anime.AnimeKai"),
    new("TotoroNext.Anime.TokyoInsider"),
    new("TotoroNext.Discord")
    {
        Dependencies =
        [
            "DiscordRPC",
            "Newtonsoft.Json"
        ]
    },
    new("TotoroNext.MediaEngine.Mpv"),
    new("TotoroNext.MediaEngine.Vlc"),
    new("TotoroNext.Anime.Aniskip"),
    new("TotoroNext.Torrents.TorBox"),
    new("TotoroNext.Torrents.RealDebrid")
};

var moduleDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "Modules");

foreach (var project in plugins)
{
    var basePath = $"../../../../../{project.Name}/bin/x64/Release/net10.0";
    CleanBin(basePath, project);
    MovePlugin(moduleDir, basePath, project);
}

return;

void CleanBin(string projectBin, PluginProject project)
{
    var files = Directory.GetFiles(projectBin, "*");
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);

        if (fileName.Contains(project.Name))
        {
            continue;
        }

        if (project.Dependencies.Any(x => fileName.Contains(x)))
        {
            continue;
        }

        File.Delete(file);
    }

    foreach (var directory in Directory.GetDirectories(projectBin))
    {
        var info = new DirectoryInfo(directory);
        info.Delete(true);
    }
}

void MovePlugin(string baseModuleDir, string projectBin, PluginProject project)
{
    var pluginDir = Path.Combine(baseModuleDir, project.Name);
    Directory.CreateDirectory(pluginDir);

    // Move files
    foreach (var filePath in Directory.GetFiles(projectBin))
    {
        var fileName = Path.GetFileName(filePath);
        var destFile = Path.Combine(pluginDir, fileName);
        File.Copy(filePath, destFile, true);
    }

    // Move subdirectories
    foreach (var dirPath in Directory.GetDirectories(projectBin))
    {
        var dirName = Path.GetFileName(dirPath);
        var destDir = Path.Combine(pluginDir, dirName);
        try
        {
            CopyDirectory(dirPath, destDir);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

void CopyDirectory(string sourceDir, string destinationDir)
{
    Directory.CreateDirectory(destinationDir);

    // Copy files
    foreach (var filePath in Directory.GetFiles(sourceDir))
    {
        var fileName = Path.GetFileName(filePath);
        var destFile = Path.Combine(destinationDir, fileName);
        File.Copy(filePath, destFile, true);
    }

    // Recursively copy subdirectories
    foreach (var subDir in Directory.GetDirectories(sourceDir))
    {
        var subDirName = Path.GetFileName(subDir);
        var destSubDir = Path.Combine(destinationDir, subDirName);
        CopyDirectory(subDir, destSubDir);
    }
}