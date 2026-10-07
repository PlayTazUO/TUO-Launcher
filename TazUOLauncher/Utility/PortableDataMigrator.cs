using System;
using System.IO;

namespace TazUOLauncher;

internal static class PortableDataMigrator
{
    internal static bool TryMigrate(string legacyRoot, string dataRoot)
    {
        string marker = Path.Combine(dataRoot, PathHelper.MigrationMarker);
        if (File.Exists(marker))
            return true;

        if (!File.Exists(Path.Combine(legacyRoot, "launcherdata.json"))
            && !Directory.Exists(Path.Combine(legacyRoot, "Profiles"))
            && !Directory.Exists(Path.Combine(legacyRoot, CONSTANTS.CLIENT_DIRECTORY_NAME)))
            return false;

        try
        {
            Directory.CreateDirectory(dataRoot);
            PathHelper.CopyIfMissing(
                Path.Combine(legacyRoot, "launcherdata.json"),
                Path.Combine(dataRoot, "launcherdata.json"));
            PathHelper.CopyDirectoryIfMissing(
                Path.Combine(legacyRoot, "Profiles"),
                Path.Combine(dataRoot, "Profiles"));
            // ponytail: copy the client synchronously; move to cancellable background migration if startup delay is noticeable.
            PathHelper.CopyDirectoryIfMissing(
                Path.Combine(legacyRoot, CONSTANTS.CLIENT_DIRECTORY_NAME),
                Path.Combine(dataRoot, CONSTANTS.CLIENT_DIRECTORY_NAME));
            File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
            return true;
        }
        catch (Exception ex)
        {
            // Keep the legacy files intact and retry missing files on the next launch.
            Console.Error.WriteLine($"Failed to migrate portable launcher data: {ex}");
            return false;
        }
    }

}
