using System;
using System.Runtime.InteropServices;

namespace TazUOLauncher;

public static class PlatformHelper
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
    public static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    
    public static bool IsMacArm => IsMac && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    
    public static string GetPlatformZipName()
    {
        if (IsWindows) return "win-x64.zip";
        if (IsLinux) return "linux-x64.zip";
        if (IsMacArm) return "osx-arm64.zip";
        if (IsMac) return "osx-x64.zip";
        return "Unknown";
    } }