using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TazUOLauncher;

public static class PathHelper
{
    internal const string MigrationMarker = ".portable-data-migrated";
    private static readonly string? appBundlePath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        ? FindAppBundle(AppDomain.CurrentDomain.BaseDirectory)
        : null;

    public static string LauncherPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
    public static string? AppBundlePath => appBundlePath;
    public static bool IsMacAppBundle => appBundlePath != null;
    public static string DataPath { get; } = GetDataPath();
    public static string ProfilesPath { get; set; } = Path.Combine(DataPath, "Profiles");
    public static string SettingsPath { get; set; } = Path.Combine(ProfilesPath, "Settings");

    /// <summary>Writable TazUO client directory; app bundles store it in Application Support.</summary>
    public static string ClientPath { get; set; } = Path.Combine(DataPath, CONSTANTS.CLIENT_DIRECTORY_NAME);

    public static void Initialize()
    {
        if (!IsMacAppBundle)
            return;

        Directory.CreateDirectory(DataPath);
        MigratePortableDataIfPresent();
    }

    public static bool CanUpdateAppBundle()
    {
        if (AppBundlePath == null)
            return true;

        string? parent = Directory.GetParent(AppBundlePath)?.FullName;
        if (parent == null)
            return false;

        string probe = Path.Combine(parent, $".tazuo-write-test-{Guid.NewGuid():N}");
        try
        {
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            File.Delete(probe);
            return true;
        }
        catch
        {
            try { File.Delete(probe); } catch { }
            return false;
        }
    }

    private static string GetDataPath()
    {
        if (appBundlePath == null)
            return LauncherPath;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "TazUO Launcher");
    }

    private static string? FindAppBundle(string baseDirectory)
    {
        for (DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(baseDirectory)); directory != null; directory = directory.Parent)
        {
            if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                return directory.FullName;
        }

        return null;
    }

    private static void MigratePortableDataIfPresent()
    {
        string? legacyRoot = Directory.GetParent(AppBundlePath!)?.FullName;
        if (legacyRoot == null || string.Equals(legacyRoot, DataPath, StringComparison.Ordinal))
            return;

        PortableDataMigrator.TryMigrate(legacyRoot, DataPath);
    }

    internal static void CopyIfMissing(string source, string destination)
    {
        if (File.Exists(source) && !File.Exists(destination) && !Directory.Exists(destination))
            File.Copy(source, destination);
    }

    internal static void CopyDirectoryIfMissing(string source, string destination)
    {
        if (!Directory.Exists(source) || File.Exists(destination))
            return;

        Directory.CreateDirectory(destination);
        foreach (string entry in Directory.EnumerateFileSystemEntries(source))
        {
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                continue;

            string target = Path.Combine(destination, Path.GetFileName(entry));
            if (Directory.Exists(entry))
                CopyDirectoryIfMissing(entry, target);
            else
                CopyIfMissing(entry, target);
        }
    }

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
            if (legacyOnly)
                return Path.Combine(ClientPath, CONSTANTS.CLASSIC_EXE_NAME + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty));

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
