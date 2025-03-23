using System;
using System.Reflection;

namespace TazUOLauncher;

internal static class LauncherVersion
{
    public static Version GetLauncherVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version != null)
            return version;

        return new Version(0, 0, 0);
    }

    public static string ToHumanReable(this Version v, bool prependv = true)
    {
        if (v == null)
            return string.Empty;

        string pv = prependv ? "v" : string.Empty;

        return $"{pv}{v.Major}.{v.Minor}.{v.Build}";
    }
}