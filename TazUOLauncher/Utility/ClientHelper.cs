using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TazUOLauncher;

internal static class ClientHelper
{
    private static Version localClientVersion = GetInstalledVersion();

    public static Version LocalClientVersion { get => localClientVersion; set { localClientVersion = GetInstalledVersion(); } }
    public static bool ExecutableExists()
    {
        return File.Exists(PathHelper.ClientExecutablePath());
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
        if (File.Exists(PathHelper.ClientExecutablePath(true)))
        {
            return AssemblyName.GetAssemblyName(PathHelper.ClientExecutablePath(true)).Version ?? new Version(0, 0, 0, 0);
        }
        return new Version(0, 0, 0, 0);
    }
}