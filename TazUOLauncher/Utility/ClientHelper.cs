using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TazUOLauncher;

internal static class ClientHelper
{
    private static Version localClientVersion = GetInstalledVersion();

    public static Version LocalClientVersion { get => localClientVersion; set { localClientVersion = GetInstalledVersion(); } }

    /// <summary>
    /// This will cleanup TazUO files when swapping channels
    /// </summary>
    public static void CleanUpClientFiles()
    {
        string[] keepDirectories = new[] { "Data", "LegionScripts", "Fonts", "ExternalImages" };
        
        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(PathHelper.ClientPath);

            if (!directoryInfo.Exists) return;

            var subDirectories = directoryInfo.GetDirectories();
            foreach (var subDirectory in subDirectories)
            {
                if (keepDirectories.Contains(subDirectory.Name)) continue;
                subDirectory.Delete(true);
            }
            
            var files = directoryInfo.GetFiles();
            foreach (var file in files)
                file.Delete();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up client files: {ex}");
        }
    }
    
    public static bool ExecutableExists(bool checkExeOnly = false)
    {
        return File.Exists(PathHelper.ClientExecutablePath(checkExeOnly));
    }
    public static void TrySetPlusXUnix()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // For example, set the executable bit for owner, group, and others.
                // Unix permissions: 0o755 => read/write/execute for owner, read/execute for group and others.
                // Note: The API uses a numeric type, so make sure you supply the correct mode.
                File.SetUnixFileMode(PathHelper.ClientExecutablePath(), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting file mode: {ex}");
            }
        }
    }
    private static Version GetInstalledVersion()
    {
        var versionTxt = Path.Combine(PathHelper.ClientPath, "v.txt");
        if (File.Exists(versionTxt))
        {
            try
            {
                var version = File.ReadAllText(versionTxt);
                return new Version(version);
            }
            catch { }
        }
        
        if (File.Exists(PathHelper.ClientExecutablePath(true)))
        {
            return AssemblyName.GetAssemblyName(PathHelper.ClientExecutablePath(true)).Version ?? new Version(0, 0, 0, 0);
        }
        
        return new Version(0, 0, 0, 0);
    }
}