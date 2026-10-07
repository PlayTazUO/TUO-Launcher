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
    /// <summary>Gets the containing macOS app bundle, or <see langword="null"/> for a non-bundled launch.</summary>
    public static string? AppBundlePath => appBundlePath;

    /// <summary>Indicates whether the launcher is running from a macOS app bundle.</summary>
    public static bool IsMacAppBundle => appBundlePath != null;

    /// <summary>Gets the directory used for writable launcher data.</summary>
    public static string DataPath { get; } = GetDataPath();
    public static string ProfilesPath { get; set; } = Path.Combine(DataPath, "Profiles");
    public static string SettingsPath { get; set; } = Path.Combine(ProfilesPath, "Settings");

    /// <summary>Writable TazUO client directory; app bundles store it in Application Support.</summary>
    public static string ClientPath { get; set; } = Path.Combine(DataPath, CONSTANTS.CLIENT_DIRECTORY_NAME);

    /// <summary>Creates the data directory and migrates recognized portable data for app bundles.</summary>
    public static void Initialize()
    {
        if (!IsMacAppBundle)
            return;

        Directory.CreateDirectory(DataPath);
        MigratePortableDataIfPresent();
    }

    /// <summary>Checks whether the app bundle's parent directory permits in-place updates.</summary>
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

    /// <summary>Returns the platform-appropriate launcher data directory.</summary>
    private static string GetDataPath()
    {
        if (appBundlePath == null)
            return LauncherPath;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "TazUO Launcher");
    }

    /// <summary>Finds the nearest enclosing <c>.app</c> directory.</summary>
    private static string? FindAppBundle(string baseDirectory)
    {
        for (DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(baseDirectory)); directory != null; directory = directory.Parent)
        {
            if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                return directory.FullName;
        }

        return null;
    }

    /// <summary>Attempts to migrate portable data from beside the app bundle.</summary>
    private static void MigratePortableDataIfPresent()
    {
        string? legacyRoot = Directory.GetParent(AppBundlePath!)?.FullName;
        if (legacyRoot == null || string.Equals(legacyRoot, DataPath, StringComparison.Ordinal))
            return;

        PortableDataMigrator.TryMigrate(legacyRoot, DataPath);
    }

    /// <summary>Copies a file only when the source exists and the destination is absent.</summary>
    internal static void CopyIfMissing(string source, string destination)
    {
        if (File.Exists(source) && !File.Exists(destination) && !Directory.Exists(destination))
            File.Copy(source, destination);
    }

    /// <summary>Copies a directory tree without overwriting existing files or following links.</summary>
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

    /// <summary>Gets the expected path to the native client executable.</summary>
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

    /// <summary>Resolves the client executable path, optionally selecting the legacy executable.</summary>
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

    /// <summary>Resolves the platform-specific native or classic client executable name.</summary>
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
