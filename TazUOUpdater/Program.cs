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
    Console.Error.WriteLine("Usage: TazUOUpdater <launcher-pid> <zip-path> <extract-dir> <launcher-exe-path>");
    return 1;
}

int pid;
if (!int.TryParse(args[0], out pid))
{
    Console.Error.WriteLine("Invalid PID.");
    return 1;
}

string zipPath = Path.GetFullPath(args[1]);
string extractDir = Path.GetFullPath(args[2]);
string launcherExePath = Path.GetFullPath(args[3]);

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

Console.WriteLine("Unzipping launcher update into launcher folder...");

// Extract the update ZIP over the launcher directory
try
{
    ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

    foreach (var file in Directory.EnumerateFiles(extractDir))
    {
        var fileInfo  = new FileInfo(file);
        
        if(fileInfo.CreationTime > DateTime.Now)
            fileInfo.CreationTime = DateTime.Now;
        
        if(fileInfo.LastWriteTime > DateTime.Now)
            fileInfo.LastWriteTime = DateTime.Now;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to extract update: {ex}");
    return 3;
}

// Clean up the temp ZIP
try { File.Delete(zipPath); } catch { /* ignore */ }

RunLauncher();

void RunLauncher()
{
    Console.WriteLine("Starting launcher...");
    // Restore execute permission on non-Windows
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        var chmod = new ProcessStartInfo
        {
            FileName = "chmod",
            UseShellExecute = false
        };
        chmod.ArgumentList.Add("+x");
        chmod.ArgumentList.Add(launcherExePath);
        Process.Start(chmod)?.WaitForExit();
    }
    
    // Relaunch the launcher
    string workingDir = Path.GetDirectoryName(launcherExePath) ?? Path.GetFullPath(".");
    // On Windows, UseShellExecute=true (ShellExecuteEx) works correctly for GUI apps.
    // On macOS/Linux, UseShellExecute=true routes through the OS file-opener (open/xdg-open)
    // which cannot launch raw Unix binaries — use false to exec directly.
    Process.Start(new ProcessStartInfo(launcherExePath)
    {
        WorkingDirectory = workingDir,
        UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    });
}

return 0;
