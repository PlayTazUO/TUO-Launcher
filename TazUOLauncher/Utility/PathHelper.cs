using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TazUOLauncher;

public static class PathHelper
{
    public static string LauncherPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

    public static string ProfilesPath { get; set; } = Path.Combine(LauncherPath, "Profiles");

    public static string SettingsPath { get; set; } = Path.Combine(ProfilesPath, "Settings");

    public static string ClientPath { get; set; } = Path.Combine(LauncherPath, CONSTANTS.CLIENT_DIRECTORY_NAME);

    public static string ClientExecutablePath()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(ClientPath, "ClassicUO.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(ClientPath, "ClassicUO.bin.x86_64");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(ClientPath, "ClassicUO.bin.osx");
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open URL: {ex.Message}");
        }
        return string.Empty;
    }
}