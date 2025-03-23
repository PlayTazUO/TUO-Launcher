using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TazUO_Launcher.Utility;

namespace TazUO_Launcher
{
    class Profile
    {
        [JsonIgnore]
        private Settings cUOSettings;

        public string Name { get; set; } = RandomWord.GenerateName(Random.Shared.Next(5, 20));
        public string SettingsFile { get; set; } = Guid.NewGuid().ToString();
        public string LastCharacterName { get; set; } = string.Empty;
        public string AdditionalArgs { get; set; } = string.Empty;

        [JsonIgnore]
        public Settings CUOSettings
        {
            get
            {
                if (cUOSettings == null)
                {
                    //LoadCUOSettings();
                    return cUOSettings;
                }
                else
                {
                    return cUOSettings;
                }
            }
            private set => cUOSettings = value;
        }

        [JsonIgnore]
        private bool isAsyncSaveStarted = false;
        [JsonIgnore]
        private DateTime asyncSaveTime;

        // private void LoadCUOSettings()
        // {
        //     if (File.Exists(GetSettingsFilePath()))
        //     {
        //         try
        //         {
        //             var data = JsonSerializer.Deserialize<Settings>(File.ReadAllText(GetSettingsFilePath()));
        //             if (data != null)
        //             {
        //                 CUOSettings = data;
        //             }
        //             else
        //             {
        //                 CUOSettings = new Settings();
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine(e.ToString());
        //             CUOSettings = new Settings();
        //         }
        //     }
        //     else
        //     {
        //         CUOSettings = new Settings();
        //     }
        // }

        public void OverrideSettings(Settings settings)
        {
            cUOSettings = settings;
        }

        /// <summary>
        /// Only the first action will be used when the save is finished. All actions in subsequent calls will be ignored.
        /// </summary>
        /// <param name="aftersave"></param>
    //     public void SaveAsync(Control flashControl = null)
    //     {
    //         asyncSaveTime = DateTime.Now + Constants.SaveProfileDelay;

    //         if (!flashControls.Contains(flashControl))
    //         {
    //             flashControls.Add(flashControl);
    //         }

    //         if (isAsyncSaveStarted)
    //         {
    //             return;
    //         }

    //         isAsyncSaveStarted = true;

    //         Task.Factory.StartNew(() =>
    //         {
    //             while (DateTime.Now < asyncSaveTime)
    //             {
    //                 Task.Delay(asyncSaveTime - DateTime.Now).Wait();
    //             }

    //             Save();

    //             isAsyncSaveStarted = false;
    //             lock (flashControl)
    //             {
    //                 foreach (Control control in flashControls)
    //                 {
    //                     Utility.Utility.UIDispatcher.Invoke(() => { ProfileWindow.FlashSaved(control); });
    //                 }

    //                 flashControls.Clear();
    //             }
    //         });
    //     }

    //     public void Save()
    //     {
    //         try
    //         {
    //             var data = JsonSerializer.Serialize(this, typeof(Profile));
    //             Directory.CreateDirectory(LauncherSettings.ProfilesPath);
    //             File.WriteAllText(GetProfileFilePath(), data);

    //             var settingsData = CUOSettings.GetSaveData();
    //             Directory.CreateDirectory(LauncherSettings.SettingsPath);
    //             File.WriteAllText(GetSettingsFilePath(), settingsData);
    //         }
    //         catch (Exception e)
    //         {
    //             Console.WriteLine($"---- Failed to save profile [ {Name} ] ---");
    //             Console.WriteLine(e.ToString());
    //             Console.WriteLine();
    //         }
    //     }

    //     public string GetSettingsFilePath()
    //     {
    //         return Path.Combine(LauncherSettings.SettingsPath, SettingsFile + ".json");
    //     }

    //     public string GetProfileFilePath()
    //     {
    //         return Path.Combine(LauncherSettings.ProfilesPath, Name + ".json");
    //     }
 }
}
