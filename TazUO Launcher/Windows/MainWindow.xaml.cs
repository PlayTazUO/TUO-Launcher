using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TazUO_Launcher.Utility;
using TazUO_Launcher.Windows;

namespace TazUO_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Profile[] allProfiles;
        private bool remoteVersionCheck = false, localVersionCheck = false;

        public MainWindow()
        {
            Task<Profile[]> getProfiles = ProfileManager.GetAllProfiles();
            bool tuoInstalled = Utility.Utility.FindTazUO();

            if (!tuoInstalled)
            {
                UpdateManager.Instance.DownloadTUO(OnDownloadProgress, () =>
                {
                    Utility.Utility.UIDispatcher.BeginInvoke(UpdateLocalVersion);
                });
            } //Start downloading TUO if it's not installed.

            InitializeComponent();

            UpdateManager.Instance.GetRemoteVersionAsync(() =>
            {
                if (UpdateManager.Instance.RemoteVersion != null)
                {
                    RemoteVersionText.Content = $"Latest TazUO version: {UpdateManager.Instance.RemoteVersion.ToString(3)}";
                    RemoteVersionText.Visibility = Visibility.Visible;

                    if (UpdateManager.Instance.MainReleaseData != null)
                    {
                        TextBlock tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Margin = new Thickness(5, 5, 5, 5);
                        tb.Text = UpdateManager.Instance.MainReleaseData.tag_name + " notes:\n" + UpdateManager.Instance.MainReleaseData.body;

                        NewsArea.Content = tb;
                    }
                }
                remoteVersionCheck = true;
            });

            UpdateManager.Instance.GetRemoteLauncherVersionAsync(() =>
            {
                if (UpdateManager.Instance.LauncherReleaseData != null)
                {
                    if (Assembly.GetExecutingAssembly().GetName().Version < UpdateManager.Instance.RemoteLauncherVersion)
                    {
                        var result = MessageBox.Show(
                            "A newer version of TazUO Launcher is available, would you like to download the update?\nYou will need to unzip the file manually, but we will open the files required to do so for you.",
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information,
                            MessageBoxResult.No
                            );

                        if (result == MessageBoxResult.Yes)
                        {
                            //Utility.Utility.OpenLauncherDownloadLink();
                            UpdateManager.Instance.DownloadLauncher(OnDownloadProgress);
                        }
                    }
                }
            });

            if (tuoInstalled)
            {
                UpdateLocalVersion();
            }

            if (!getProfiles.IsCompleted) //This should be extremely fast
            {
                getProfiles.Wait();
            }

            allProfiles = getProfiles.Result;

            foreach (Profile profile in allProfiles)
            {
                ProfileSelector.Items.Add(new ComboBoxItem() { Content = profile.Name, Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)) });
            }

            ProfileSelector.SelectedIndex = LauncherSettings.LastSelectedProfileIndex;

            ProfileSelector.SelectionChanged += (s, e) =>
            {
                LauncherSettings.LastSelectedProfileIndex = ProfileSelector.SelectedIndex;
            };

            Task.Factory.StartNew(() =>
            {
                while (!remoteVersionCheck || !localVersionCheck)
                {
                    Task.Delay(1000).Wait();
                }
                if (
                    UpdateManager.Instance.LocalVersion == null ||
                        (
                            UpdateManager.Instance.RemoteVersion != null &&
                            UpdateManager.Instance.LocalVersion != null &&
                            UpdateManager.Instance.LocalVersion < UpdateManager.Instance.RemoteVersion
                        )
                )
                {
                    Utility.Utility.UIDispatcher.BeginInvoke(() => { DownloadUpdateButton.Visibility = Visibility.Visible; });
                }
            });
        }

        private void ProfileSettingsButtonMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProfileWindow profileWindow = new ProfileWindow();
            profileWindow.Show();
            profileWindow.Closed += (s, e) =>
            {
                Task<Profile[]> getProfiles = ProfileManager.GetAllProfiles();

                if (!getProfiles.IsCompleted) //This should be extremely fast
                {
                    getProfiles.Wait();
                }

                allProfiles = getProfiles.Result;

                ProfileSelector.Items.Clear();

                foreach (Profile profile in allProfiles)
                {
                    ProfileSelector.Items.Add(new ComboBoxItem() { Content = profile.Name, Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)) });
                }

                ProfileSelector.SelectedIndex = LauncherSettings.LastSelectedProfileIndex;
            };
        }

        private void DiscordIconMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var destinationurl = "https://discord.gg/QvqzkB95G4";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }

        private void GithubIconMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var destinationurl = "https://github.com/bittiez/TazUO";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }

        private void WikiMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var destinationurl = "https://github.com/bittiez/TazUO/wiki";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }

        private void DownloadButtonPressed(object sender, RoutedEventArgs e)
        {
            DownloadUpdateButton.Visibility = Visibility.Hidden;

            UpdateManager.Instance.DownloadTUO(OnDownloadProgress, () =>
            {
                Utility.Utility.UIDispatcher.BeginInvoke(UpdateLocalVersion);
            });
        }

        private void OnDownloadProgress(int p)
        {
            Console.WriteLine(p.ToString());
            if (p > 0 && p < 100)
            {
                DownloadProgressBar.Value = p;
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressLabel.Visibility = Visibility.Visible;
            }

            if (p >= 100)
            {
                DownloadProgressBar.Visibility = Visibility.Hidden;
                DownloadProgressLabel.Visibility = Visibility.Hidden;
            }
        }

        private void ShowNotesButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (NewsArea.Visibility == Visibility.Hidden || NewsArea.Visibility == Visibility.Collapsed)
            {
                NewsArea.Visibility = Visibility.Visible;
                NewsAreaBorder.Visibility = Visibility.Visible;
            }
            else
            {
                NewsArea.Visibility = Visibility.Collapsed;
                NewsAreaBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void PlayButtonClicked(object sender, RoutedEventArgs e)
        {
            if (Utility.Utility.FindTazUO())
            {
                if (ProfileSelector.SelectedIndex > -1 && !UpdateManager.Instance.DownloadInProgress)
                {
                    string tuoExecutable = Utility.Utility.GetTazUOExecutable();

                    if (ProfileManager.TryFindProfile(((ComboBoxItem)ProfileSelector.SelectedItem).Content.ToString(), out Profile? profile))
                    {
                        try
                        {
                            var proc = new ProcessStartInfo(tuoExecutable, $"-settings \"{profile.GetSettingsFilePath()}\"");
                            proc.Arguments += " -skipupdatecheck";
                            if (profile.CUOSettings.AutoLogin && !string.IsNullOrEmpty(profile.LastCharacterName))
                            {
                                proc.Arguments += $" -lastcharactername \"{profile.LastCharacterName}\"";
                            }
                            if (profile.CUOSettings.AutoLogin)
                            {
                                proc.Arguments += " -skiploginscreen";
                            }
                            if (!string.IsNullOrEmpty(profile.AdditionalArgs))
                            {
                                proc.Arguments += " " + profile.AdditionalArgs;
                            }
                            Process.Start(proc);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            else
            {
                //Do update stuff here
            }
        }

        private void ImportDataFolder(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "This will copy the contents of the folder you select into your TazUO Data folder.\n\n" +
                "You need to select the Data folder of a CLASSICUO or TAZUO folder, NOT the Data folder of an official EA UO installation.\n\n" +
                "The folder you are looking for should contain a Client and Profiles folder, select the Data folder containing these, not these sub-folders.\n\n" +
                "!! Make sure your TazUO client is not running. !!\n\n" +
                "Do you understand and want to continue?", "Warning", MessageBoxButton.YesNo)
                ;

            if (result == MessageBoxResult.Yes)
            {
                string folderPath = Utility.Utility.AskForFolder();

                if (Directory.Exists(folderPath)) //Selected folder exists
                {
                    if (Directory.Exists(Path.Combine(folderPath, "Profiles"))) //Selected folder has a Profiles folder, more likely to be the correct folder
                    {
                        string tuoDir = Path.Combine(LauncherSettings.LauncherPath + "TazUO");
                        if (Directory.Exists(tuoDir))
                        {
                            try
                            {
                                Utility.Utility.DirectoryCopy(folderPath, Path.Combine(tuoDir, "Data"));
                                MessageBox.Show("Succesfully copied your data folder over.");
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show("There was an error while copying your files over\n\n" + exception.Message);
                            }
                        }
                    }
                }
            }
        }

        private void ImportClassicUOProfiles(object sender, RoutedEventArgs e)
        {
            Utility.Utility.ImportCUOProfiles();

            Task<Profile[]> getProfiles = ProfileManager.GetAllProfiles();

            if (!getProfiles.IsCompleted) //This should be extremely fast
            {
                getProfiles.Wait();
            }

            allProfiles = getProfiles.Result;

            ProfileSelector.Items.Clear();

            foreach (Profile profile in allProfiles)
            {
                ProfileSelector.Items.Add(new ComboBoxItem() { Content = profile.Name, Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)) });
            }

            ProfileSelector.SelectedIndex = LauncherSettings.LastSelectedProfileIndex;
        }

        private void ForceDownloadLatest(object sender, RoutedEventArgs e)
        {
            DownloadButtonPressed(sender, e);
        }

        private void DownloadBleedingEdge(object sender, RoutedEventArgs e)
        {
            UpdateManager.Instance.DownloadLatestBleedingEdge(OnDownloadProgress, () =>
            {
                Utility.Utility.UIDispatcher.BeginInvoke(UpdateLocalVersion);
            });
        }

        private void ToolsMenu_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToolsMenu.IsOpen = true;
            e.Handled = true;
        }

        private void UpdateLocalVersion()
        {
            Version? l = UpdateManager.Instance.GetInstalledVersion(Utility.Utility.GetTazUOExecutable());
            if (l != null)
            {
                LocalVersionText.Content = $"Your TazUO version: {l.ToString(3)}";
                LocalVersionText.Visibility = Visibility.Visible;
            }
            localVersionCheck = true;
        }
    }
}
