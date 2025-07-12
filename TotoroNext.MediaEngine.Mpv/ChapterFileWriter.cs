using System.Text;
using TotoroNext.MediaEngine.Abstractions;
using Path = System.IO.Path;

namespace TotoroNext.MediaEngine.Mpv;

internal class ChapterFileWriter
{
    public static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "Modules",
                     typeof(ChapterFileWriter).Assembly.GetName().Name!, "chapters.txt");

    internal static string CreateChapterFile(IReadOnlyList<MediaSegment> sections)
    {
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");
        foreach (var section in sections)
        {
            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/60");
            sb.AppendLine($"START={(int)section.Start.TotalSeconds * 60}");
            sb.AppendLine($"END={(int)section.End.TotalSeconds * 60}");
            if (section.Type != MediaSectionType.Content)
            {
                sb.AppendLine($"title={section.Type}");
            }
        }

        File.WriteAllText(FilePath, sb.ToString());

        return FilePath;
    }
}