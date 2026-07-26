using System.Runtime.Versioning;
using Microsoft.Win32;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Mpv;

[SupportedOSPlatform("windows")]
public class WindowsInitializer(IModuleSettings<Settings> settings) : IInitializer
{
    public void Initialize()
    {
        if (!string.IsNullOrEmpty(settings.Value.FileName))
        {
            return;
        }

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\mpv.exe");
        if (key is null)
        {
            return;
        }

        var value = key.GetValue("")?.ToString();

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        settings.Value.FileName = value;
        settings.Save();
    }
}