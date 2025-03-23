using System;
using System.IO;
using System.Reflection;

namespace TazUOLauncher;

internal static class ClientHelper
{
    public static Version LocalClientVersion { get; set; } = GetInstalledVersion();
    public static bool ExecutableExists()
    {
        return File.Exists(PathHelper.ClientExecutablePath());
    }

    private static Version GetInstalledVersion()
    {
        if (File.Exists(PathHelper.ClientExecutablePath()))
        {
            return AssemblyName.GetAssemblyName(PathHelper.ClientExecutablePath()).Version ?? new Version(0, 0, 0, 0);
        }
        return new Version(0, 0, 0, 0);
    }
}