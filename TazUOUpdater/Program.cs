using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

Console.Title = "TazUO Updater";
Console.WriteLine("Running TazUO Updater...");

if (args.Length < 4)
{
    Console.Error.WriteLine("Usage: TazUOUpdater <launcher-pid> <zip-path> <update-directory> <launcher-exe-or-app-path> [release-url]");
    return 1;
}

if (!int.TryParse(args[0], out int pid))
{
    Console.Error.WriteLine("Invalid PID.");
    return 1;
}

string zipPath = Path.GetFullPath(args[1]);
string updateDirectory = Path.GetFullPath(args[2]);
string launcherPath = Path.GetFullPath(args[3]);
string fallbackUrl = args.Length > 4 ? args[4] : "https://github.com/PlayTazUO/TUO-Launcher/releases/latest";
bool isMacAppBundle = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
    && Directory.Exists(launcherPath)
    && Path.GetExtension(launcherPath).Equals(".app", StringComparison.OrdinalIgnoreCase);

// Wait for the launcher to exit
try
{
    Console.WriteLine("Waiting for launcher to fully exit...");
    var proc = Process.GetProcessById(pid);
    if (!proc.WaitForExit(10_000))
    {
        RunLauncher();
        return 3;
    }
}
catch (ArgumentException)
{
    // Process already exited — that's fine
}

// Give the OS a moment to release file handles
Thread.Sleep(500);

Console.WriteLine(isMacAppBundle
    ? "Replacing the launcher app bundle..."
    : "Unzipping launcher update into launcher folder...");

try
{
    if (isMacAppBundle)
    {
        UpdateMacAppBundle(zipPath, updateDirectory, launcherPath);
    }
    else
    {
        ZipFile.ExtractToDirectory(zipPath, updateDirectory, overwriteFiles: true);
        FixFutureTimestamps(updateDirectory);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to install launcher update: {ex}");
    if (isMacAppBundle)
    {
        OpenReleasePage();
        RunLauncher();
    }
    return 3;
}

// Clean up the temp ZIP
try { File.Delete(zipPath); } catch { /* ignore */ }

RunLauncher();
return 0;

void UpdateMacAppBundle(string archivePath, string parentDirectory, string appBundlePath)
{
    string actualParent = Path.GetDirectoryName(appBundlePath)
        ?? throw new InvalidOperationException("The app bundle has no parent directory.");
    if (!string.Equals(Path.GetFullPath(parentDirectory), actualParent, StringComparison.Ordinal))
        throw new InvalidOperationException("The update destination does not match the app bundle location.");

    string stagingDirectory = Path.Combine(parentDirectory, $".tazuo-update-{Guid.NewGuid():N}");
    Directory.CreateDirectory(stagingDirectory);
    try
    {
        ZipFile.ExtractToDirectory(archivePath, stagingDirectory);

        // Keep the user's existing .app name even if they renamed it in Finder.
        string stagedBundle = Path.Combine(stagingDirectory, "TazUOLauncher.app");
        string executable = Path.Combine(stagedBundle, "Contents", "MacOS", "TazUOLauncher");
        string updater = Path.Combine(stagedBundle, "Contents", "MacOS", "update", "TazUOUpdater");
        if (!File.Exists(Path.Combine(stagedBundle, "Contents", "Info.plist"))
            || !File.Exists(executable)
            || !File.Exists(updater))
        {
            throw new InvalidDataException("The downloaded archive is missing required app bundle files.");
        }

        SetExecutable(executable);
        SetExecutable(updater);

        string backupBundle = appBundlePath + $".backup-{Guid.NewGuid():N}";
        Directory.Move(appBundlePath, backupBundle);
        try
        {
            Directory.Move(stagedBundle, appBundlePath);
        }
        catch
        {
            Directory.Move(backupBundle, appBundlePath);
            throw;
        }

        try { Directory.Delete(backupBundle, recursive: true); }
        catch (Exception ex) { Console.Error.WriteLine($"Could not remove previous app bundle: {ex.Message}"); }
    }
    finally
    {
        try { Directory.Delete(stagingDirectory, recursive: true); } catch { /* ignore */ }
    }
}

void SetExecutable(string path)
{
    File.SetUnixFileMode(path,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
}

void FixFutureTimestamps(string directory)
{
    foreach (string file in Directory.EnumerateFiles(directory))
    {
        var fileInfo = new FileInfo(file);
        if (fileInfo.CreationTime > DateTime.Now)
            fileInfo.CreationTime = DateTime.Now;
        if (fileInfo.LastWriteTime > DateTime.Now)
            fileInfo.LastWriteTime = DateTime.Now;
    }
}

void OpenReleasePage()
{
    try
    {
        var open = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };
        open.ArgumentList.Add(fallbackUrl);
        Process.Start(open);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Could not open the release page: {ex.Message}");
    }
}

void RunLauncher()
{
    Console.WriteLine("Starting launcher...");
    if (isMacAppBundle)
    {
        var open = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };
        open.ArgumentList.Add("-n");
        open.ArgumentList.Add(launcherPath);
        open.WorkingDirectory = updateDirectory;
        Process.Start(open);
        return;
    }

    // Restore execute permission on non-Windows
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        var chmod = new ProcessStartInfo
        {
            FileName = "chmod",
            UseShellExecute = false
        };
        chmod.ArgumentList.Add("+x");
        chmod.ArgumentList.Add(launcherPath);
        Process.Start(chmod)?.WaitForExit();
    }

    // On Windows, UseShellExecute=true (ShellExecuteEx) works correctly for GUI apps.
    // On macOS/Linux, UseShellExecute=true routes through the OS file-opener (open/xdg-open)
    // which cannot launch raw Unix binaries — use false to exec directly.
    Process.Start(new ProcessStartInfo(launcherPath)
    {
        WorkingDirectory = Path.GetDirectoryName(launcherPath) ?? Path.GetFullPath("."),
        UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    });
}
