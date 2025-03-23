using System;
using TazUO_Launcher.Utility;

namespace TazUOLauncher;

internal static class Utility
{
    public static Version GetVersion(this GitHubReleaseData data)
    {
        if (data != null && data.name != null)
        {
            if (data.name.StartsWith('v'))
            {
                data.name = data.name.Substring(1);
            }

            if (Version.TryParse(data.name, out var version))
            {
                return version;
            }
        }
        return new Version(0, 0, 0);
    }
}



// using System;
// using System.IO;
// using System.Xml;

// namespace TazUO_Launcher.Utility
// {
//     class Utility
//     {
//         //public static Dispatcher UIDispatcher = Application.Current.Dispatcher;


//         // public static string AskForFile(string intialDirectory, string fileFilter = "")
//         // {
//         //     OpenFileDialog openFileDialog = new OpenFileDialog
//         //     {
//         //         InitialDirectory = intialDirectory,
//         //         CheckFileExists = true,
//         //         CheckPathExists = true
//         //     };
//         //     if(!string.IsNullOrEmpty(fileFilter))
//         //         openFileDialog.Filter = fileFilter;

//         //     var result = openFileDialog.ShowDialog();
//         //     if (result == true)
//         //     {
//         //         return openFileDialog.FileName;
//         //     }
//         //     else
//         //     {
//         //         return string.Empty;
//         //     }
//         // }

//         // public static string AskForFolder()
//         // {
//         //     Ookii.Dialogs.Wpf.VistaFolderBrowserDialog folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

//         //     if (folderBrowserDialog.ShowDialog() == true)
//         //     {
//         //         return folderBrowserDialog.SelectedPath;
//         //     }
//         //     else
//         //     {
//         //         return string.Empty;
//         //     }
//         // }

//         public static void OpenLauncherDownloadLink()
//         {
//             var destinationurl = "https://github.com/bittiez/TUO-Launcher/releases/latest";
//             var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
//             {
//                 UseShellExecute = true,
//             };
//             System.Diagnostics.Process.Start(sInfo);
//         }

//         public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
//         {
//             // Get the subdirectories for the specified directory.
//             DirectoryInfo dir = new DirectoryInfo(sourceDirName);

//             if (!dir.Exists)
//             {
//                 throw new DirectoryNotFoundException(
//                     "Source directory does not exist or could not be found: "
//                     + sourceDirName);
//             }

//             DirectoryInfo[] dirs = dir.GetDirectories();
//             // If the destination directory doesn't exist, create it.
//             if (!Directory.Exists(destDirName))
//             {
//                 Directory.CreateDirectory(destDirName);
//             }

//             // Get the files in the directory and copy them to the new location.
//             FileInfo[] files = dir.GetFiles();
//             foreach (FileInfo file in files)
//             {
//                 string temppath = Path.Combine(destDirName, file.Name);
//                 file.CopyTo(temppath, true);
//             }

//             // If copying subdirectories, copy them and their contents to new location.
//             if (copySubDirs)
//             {
//                 foreach (DirectoryInfo subdir in dirs)
//                 {
//                     string temppath = Path.Combine(destDirName, subdir.Name);
//                     DirectoryCopy(subdir.FullName, temppath, copySubDirs);
//                 }
//             }
//         }

//         public static void ImportCUOProfiles()
//         {
//             string CUOPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClassicUOLauncher", "launcher_settings.xml");
//             if (File.Exists(CUOPath))
//             {
//                 try
//                 {
//                     Profile newProfile = new Profile();

//                     while (ProfileManager.TryFindProfile(newProfile.Name, out _))
//                     {
//                         newProfile.Name += "x";
//                     }

//                     XmlDocument cuoLauncher = new XmlDocument();
//                     cuoLauncher.Load(CUOPath);

//                     XmlNode? root = cuoLauncher.DocumentElement;


//                     if (root != null)
//                     {
//                         XmlNode? profiles = root["profiles"];
//                         if (profiles != null)
//                         {
//                             foreach (XmlNode profile in profiles.ChildNodes)
//                             {
//                                 if (profile.Name == "profile")
//                                 {
//                                     foreach (XmlAttribute attr in profile.Attributes)
//                                     {
//                                         switch (attr.Name)
//                                         {
//                                             case "name":
//                                                 newProfile.Name = attr.Value;
//                                                 while (ProfileManager.TryFindProfile(newProfile.Name, out _))
//                                                 {
//                                                     newProfile.Name += "x";
//                                                 }
//                                                 break;
//                                             case "username":
//                                                 newProfile.CUOSettings.Username = attr.Value;
//                                                 break;
//                                             case "password":
//                                                 newProfile.CUOSettings.Password = attr.Value;
//                                                 break;
//                                             case "server":
//                                                 newProfile.CUOSettings.IP = attr.Value;
//                                                 break;
//                                             case "port":
//                                                 if (ushort.TryParse(attr.Value, out ushort port))
//                                                 {
//                                                     newProfile.CUOSettings.Port = port;
//                                                 }
//                                                 break;
//                                             case "charname":
//                                                 newProfile.LastCharacterName = attr.Value;
//                                                 break;
//                                             case "client_version":
//                                                 newProfile.CUOSettings.ClientVersion = attr.Value;
//                                                 break;
//                                             case "uopath":
//                                                 newProfile.CUOSettings.UltimaOnlineDirectory = attr.Value;
//                                                 break;
//                                             case "last_server_index":
//                                                 if (ushort.TryParse(attr.Value, out ushort lserver))
//                                                 {
//                                                     newProfile.CUOSettings.LastServerNum = lserver;

//                                                 }
//                                                 break;
//                                             case "last_server_name":
//                                                 newProfile.CUOSettings.LastServerName = attr.Value;
//                                                 break;
//                                             case "save_account":
//                                                 if (bool.TryParse(attr.Value, out bool sacount))
//                                                 {
//                                                     newProfile.CUOSettings.SaveAccount = sacount;
//                                                 }
//                                                 break;
//                                             case "autologin":
//                                                 if (bool.TryParse(attr.Value, out bool autolog))
//                                                 {
//                                                     newProfile.CUOSettings.AutoLogin = autolog;
//                                                 }
//                                                 break;
//                                             case "reconnect":
//                                                 if (bool.TryParse(attr.Value, out bool recon))
//                                                 {
//                                                     newProfile.CUOSettings.Reconnect = recon;
//                                                 }
//                                                 break;
//                                             case "reconnect_time":
//                                                 if (int.TryParse(attr.Value, out int n))
//                                                 {
//                                                     newProfile.CUOSettings.ReconnectTime = n;
//                                                 }
//                                                 break;
//                                             case "has_music":
//                                                 if (bool.TryParse(attr.Value, out bool nn))
//                                                 {
//                                                     newProfile.CUOSettings.LoginMusic = nn;
//                                                 }
//                                                 break;
//                                             case "use_verdata":
//                                                 if (bool.TryParse(attr.Value, out bool nnn))
//                                                 {
//                                                     newProfile.CUOSettings.UseVerdata = nnn;
//                                                 }
//                                                 break;
//                                             case "music_volume":
//                                                 if (int.TryParse(attr.Value, out int nnnn))
//                                                 {
//                                                     newProfile.CUOSettings.LoginMusicVolume = nnnn;
//                                                 }
//                                                 break;
//                                             case "encryption_type":
//                                                 if (byte.TryParse(attr.Value, out byte nnnnn))
//                                                 {
//                                                     newProfile.CUOSettings.Encryption = nnnnn;
//                                                 }
//                                                 break;
//                                             case "force_driver":
//                                                 if (byte.TryParse(attr.Value, out byte nnnnnn))
//                                                 {
//                                                     newProfile.CUOSettings.ForceDriver = nnnnnn;
//                                                 }
//                                                 break;
//                                             case "args":
//                                                 newProfile.AdditionalArgs = attr.Value;
//                                                 break;
//                                         }
//                                     }
//                                 }
//                             }
//                             //newProfile.Save();
//                             //MessageBox.Show($"Imported {profiles.ChildNodes.Count} profiles from ClassicUO Launcher!");
//                             return;
//                         }
//                     }

//                 }
//                 catch (Exception e)
//                 {
//                     //MessageBox.Show("Failed to import ClassicUO Launcher profiles.\n\n" + e.Message);
//                 }
//             }
//             else
//             {
//                 //MessageBox.Show("Could not find any ClassicUO Launcher profiles to import.");
//             }

//             //MessageBox.Show("Failed to import ClassicUO Launcher profiles.");
//         }
//     }
// }
