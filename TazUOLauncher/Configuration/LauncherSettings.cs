using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TazUOLauncher;

internal class LauncherSettings
{
    public static LauncherSaveFile GetLauncherSaveFile { get; } = LauncherSaveFile.Get();
    public static Version LocalLauncherVersion { get; } = LauncherVersion.GetLauncherVersion();

    internal class LauncherSaveFile
    {
        public string LastSelectedProfileName { get; set; } = string.Empty;
        public ReleaseChannel DownloadChannel { get; set; } = ReleaseChannel.MAIN;
        public bool AutoDownloadUpdates { get; set; } = false;

        public static LauncherSaveFile Get()
        {
            try
            {
                var p = Path.Combine(PathHelper.LauncherPath, "launcherdata.json");
                if (File.Exists(p))
                {
                    return JsonSerializer.Deserialize<LauncherSaveFile>(File.ReadAllText(p)) ?? new LauncherSaveFile();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new LauncherSaveFile();
        }

        public async Task Save()
        {
            await Task.Run(() =>
            {
                try
                {
                    var targetPath = Path.Combine(PathHelper.LauncherPath, "launcherdata.json");
                    var tempPath = targetPath + ".tmp";
                    
                    File.WriteAllText(tempPath, JsonSerializer.Serialize<LauncherSaveFile>(this));
                    File.Move(tempPath, targetPath, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
        public LauncherSaveFile() { }
    }
}
