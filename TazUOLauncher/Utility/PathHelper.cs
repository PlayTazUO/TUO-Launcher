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

    public static string NativeClientPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(ClientPath, CONSTANTS.NATIVE_EXECUTABLE_NAME + ".exe");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(ClientPath, CONSTANTS.NATIVE_EXECUTABLE_NAME);
        }

        return string.Empty;
    }
    public static string ClientExecutablePath(bool returnExeOnly = false, bool legacyOnly = false)
    {
        try
        {
            if(legacyOnly)
                return Path.Combine(ClientPath, CONSTANTS.CLASSIC_EXE_NAME + ".exe");
            
            return NativePath(returnExeOnly);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to find path: {ex.Message}");
        }

        return string.Empty;
    }

    private static string NativePath(bool returnExeOnly)
    {
        string exeName;

        if (returnExeOnly || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            exeName = CONSTANTS.NATIVE_EXECUTABLE_NAME + ".exe";
            if (!File.Exists(Path.Combine(ClientPath, exeName)))
                exeName = CONSTANTS.CLASSIC_EXE_NAME + ".exe";

            return Path.Combine(ClientPath, exeName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            exeName = CONSTANTS.NATIVE_EXECUTABLE_NAME;
            if (!File.Exists(Path.Combine(ClientPath, exeName)))
                exeName = CONSTANTS.CLASSIC_EXE_NAME;

            return Path.Combine(ClientPath, exeName);
        }

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }
}