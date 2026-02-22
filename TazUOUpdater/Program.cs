using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

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

string zipPath = args[1];
string extractDir = args[2];
string launcherExePath = args[3];

// Wait for the launcher to exit
try
{
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
    Process.Start(new ProcessStartInfo(launcherExePath) { WorkingDirectory = new FileInfo(launcherExePath).DirectoryName, UseShellExecute = true });
}

return 0;
