using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TazUO_Launcher.Utility;

namespace TazUO_Launcher.Windows
{
    /// <summary>
    /// Interaction logic for ProfileWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        private Profile selectedProfile;

        public ProfileWindow()
        {
            Task<Profile[]> loadProfiles = ProfileManager.GetAllProfiles();

            InitializeComponent();

            if (!loadProfiles.IsCompleted)
            {
                loadProfiles.Wait();
            }

            foreach (Profile profile in loadProfiles.Result)
            {
                ProfileList.Items.Add(new ListBoxItem() { Content = profile.Name });
            }

            ProfileEditArea.IsEnabled = false;

            ButtonNew.Click += ButtonNew_MouseUp;
            ButtonDelete.Click += ButtonDelete_MouseUp;
            ButtonCopy.Click += ButtonCopy_MouseUp;

            ProfileList.SelectionChanged += ProfileList_SelectionChanged;

            LocateUOButton.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {

                    string selectDir = Utility.Utility.AskForFile(EntryUODirectory.Text, "UO Client (*.exe)|*.exe");
                    if (!string.IsNullOrEmpty(selectDir))
                    {
                        EntryUODirectory.Text = Path.GetDirectoryName(selectDir);
                        selectedProfile.CUOSettings.UltimaOnlineDirectory = EntryUODirectory.Text;

                        if (ClientVersionHelper.TryParseFromFile(Path.Combine(selectedProfile.CUOSettings.UltimaOnlineDirectory, "client.exe"), out string version))
                        {
                            EntryClientVersion.Text = version;
                            selectedProfile.CUOSettings.ClientVersion = version;
                        }

                        selectedProfile.Save();
                    }
                }
            };

            PluginListButtonAdd.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    string path = Utility.Utility.AskForFile("", "Assistants (*.exe, *.dll)|*.exe;*.dll");
                    if (!string.IsNullOrEmpty(path))
                    {
                        EntryPluginList.Items.Add(new ListBoxItem() { Content = path });
                        SavePluginList();
                    }
                }
            };

            PluginListButtonRemove.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (EntryPluginList.SelectedIndex > -1)
                    {
                        EntryPluginList.Items.RemoveAt(EntryPluginList.SelectedIndex);
                        SavePluginList();
                    }
                }
            };

            PresetServerDropdown.SelectionChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    string serv = (string)((ComboBoxItem)PresetServerDropdown.SelectedItem).Tag;
                    if (!string.IsNullOrEmpty(serv))
                    {
                        EntryServerIP.Text = serv.Split(":")[0];
                        EntryServerPort.Text = serv.Split(":")[1];
                    }
                }
            };

            SetUpSaveMethods();
        }

        private void SetUpSaveMethods()
        {
            EntryProfileName.TextChanged += (s, e) =>
            {
                if (selectedProfile != null && EntryProfileName.Text != "")
                {
                    if (!selectedProfile.Name.Equals(EntryProfileName.Text))
                    {
                        while (ProfileManager.TryFindProfile(EntryProfileName.Text, out _))
                        {
                            EntryProfileName.Text += "x";
                        }
                        ProfileManager.DeleteProfileFile(selectedProfile);
                        selectedProfile.Name = EntryProfileName.Text;
                        selectedProfile.SaveAsync(EntryProfileName);
                        ((ListBoxItem)ProfileList.SelectedItem).Content = selectedProfile.Name;
                    }
                }
            };

            EntryAccountName.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.Username.Equals(EntryAccountName.Text))
                    {
                        selectedProfile.CUOSettings.Username = EntryAccountName.Text;
                        selectedProfile.SaveAsync(EntryAccountName);
                        if(!string.IsNullOrEmpty(EntryAccountName.Text))
                        {
                            selectedProfile.CUOSettings.SaveAccount = true;
                            EntrySavePass.IsChecked = true;
                        }
                    }
                }
            };
            EntryAccountPass.PasswordChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!Crypter.Decrypt(selectedProfile.CUOSettings.Password).Equals(EntryAccountPass.Password))
                    {
                        selectedProfile.CUOSettings.Password = Crypter.Encrypt(EntryAccountPass.Password);
                        selectedProfile.SaveAsync(EntryAccountPass);
                        if (!string.IsNullOrEmpty(EntryAccountPass.Password))
                        {
                            selectedProfile.CUOSettings.SaveAccount = true;
                            EntrySavePass.IsChecked = true;
                        }
                    }
                }
            };
            EntrySavePass.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.SaveAccount.Equals(EntrySavePass.IsChecked))
                    {
                        selectedProfile.CUOSettings.SaveAccount = (bool)(EntrySavePass.IsChecked == null ? false : EntrySavePass.IsChecked);
                        selectedProfile.SaveAsync(EntrySavePass);
                        if (!selectedProfile.CUOSettings.SaveAccount)
                        {
                            EntryAccountName.Text = "";
                            EntryAccountPass.Password = "";
                        }
                    }
                }
            };

            EntryServerIP.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.IP.Equals(EntryServerIP.Text))
                    {
                        selectedProfile.CUOSettings.IP = EntryServerIP.Text;
                        selectedProfile.SaveAsync(EntryServerIP);
                    }
                }
            };
            EntryServerPort.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.Port.ToString().Equals(EntryServerPort.Text))
                    {
                        if (ushort.TryParse(EntryServerPort.Text, out var port))
                        {
                            selectedProfile.CUOSettings.Port = port;
                            selectedProfile.SaveAsync(EntryServerPort);
                        }
                    }
                }
            };

            EntryUODirectory.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.UltimaOnlineDirectory.Equals(EntryUODirectory.Text))
                    {
                        selectedProfile.CUOSettings.UltimaOnlineDirectory = EntryUODirectory.Text;

                        if (ClientVersionHelper.TryParseFromFile(Path.Combine(selectedProfile.CUOSettings.UltimaOnlineDirectory, "client.exe"), out string version))
                        {
                            EntryClientVersion.Text = version;
                            selectedProfile.CUOSettings.ClientVersion = version;
                        }

                        selectedProfile.SaveAsync(EntryUODirectory);
                    }
                }
            };
            EntryClientVersion.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.ClientVersion.Equals(EntryClientVersion.Text))
                    {
                        selectedProfile.CUOSettings.ClientVersion = EntryClientVersion.Text;
                        selectedProfile.SaveAsync(EntryClientVersion);
                    }
                }
            };
            EntryClientVersion.TextChanged += (s, e) =>
            {
                if (ClientVersionHelper.IsClientVersionValid(EntryClientVersion.Text, out var version))
                {
                    EntryClientVersion.BorderBrush = Brushes.DarkGreen;
                }
                else
                {
                    EntryClientVersion.BorderBrush = Brushes.DarkRed;
                }
            };
            EntryEncrypedClient.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    //Need to fix, this will never be true
                    if (!selectedProfile.CUOSettings.Encryption.Equals(EntryEncrypedClient.IsChecked))
                    {
                        if (EntryEncrypedClient.IsChecked != null && (bool)EntryEncrypedClient.IsChecked)
                        {
                            selectedProfile.CUOSettings.Encryption = 1;
                        }
                        else
                        {
                            selectedProfile.CUOSettings.Encryption = 0;
                        }

                        selectedProfile.SaveAsync(EntryEncrypedClient);
                    }
                }
            };

            EntryAutoLogin.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.AutoLogin.Equals(EntryAutoLogin.IsChecked))
                    {
                        if (EntryAutoLogin.IsChecked == true)
                        {
                            selectedProfile.CUOSettings.AutoLogin = true;
                        }
                        else
                        {
                            selectedProfile.CUOSettings.AutoLogin = false;
                        }
                        selectedProfile.SaveAsync(EntryAutoLogin);
                    }
                }
            };
            EntryReconnect.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.Reconnect.Equals(EntryReconnect.IsChecked))
                    {
                        if (EntryReconnect.IsChecked == true)
                        {
                            selectedProfile.CUOSettings.Reconnect = true;
                        }
                        else
                        {
                            selectedProfile.CUOSettings.Reconnect = false;
                        }
                        selectedProfile.SaveAsync(EntryReconnect);
                    }
                }
            };
            EntryReconnectTime.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.Reconnect.ToString().Equals(EntryReconnectTime.Text.ToString()))
                    {
                        if (int.TryParse(EntryReconnectTime.Text, out int ms))
                        {
                            selectedProfile.CUOSettings.ReconnectTime = ms;
                            selectedProfile.SaveAsync(EntryReconnectTime);
                        }
                    }
                }
            };
            EntryLoginMusic.Click += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.LoginMusic.Equals(EntryLoginMusic.IsChecked))
                    {
                        if (EntryLoginMusic.IsChecked == true)
                        {
                            selectedProfile.CUOSettings.LoginMusic = true;
                        }
                        else
                        {
                            selectedProfile.CUOSettings.LoginMusic = false;
                        }
                        selectedProfile.SaveAsync(EntryLoginMusic);
                    }
                }
            };
            EntryMusicVolume.ValueChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!selectedProfile.CUOSettings.LoginMusicVolume.Equals((int)EntryMusicVolume.Value))
                    {
                        selectedProfile.CUOSettings.LoginMusicVolume = (int)EntryMusicVolume.Value;
                        selectedProfile.SaveAsync(EntryMusicVolume);
                    }
                }
            };

            EntryLastCharName.TextChanged += (s, e) =>
            {
                if (selectedProfile != null)
                {
                    if (!EntryLastCharName.Text.Equals(selectedProfile.LastCharacterName))
                    {
                        selectedProfile.LastCharacterName = EntryLastCharName.Text;
                        selectedProfile.SaveAsync(EntryLastCharName);
                    }
                }
            };

            EntryAdditionalArgs.TextChanged += (s, e) =>
            {
                if(selectedProfile != null)
                {
                    selectedProfile.AdditionalArgs = EntryAdditionalArgs.Text;
                    selectedProfile.SaveAsync(EntryAdditionalArgs);
                }
            };
        }

        /// <summary>
        /// Do not call in ui thread, this will lock the thread.
        /// </summary>
        /// <param name="c"></param>
        public static void FlashSaved(Control c)
        {
            if (c != null)
            {
                Brush old = c.BorderBrush;
                var size = c.BorderThickness;

                Utility.Utility.UIDispatcher.BeginInvoke(() =>
                {
                    c.BorderBrush = Brushes.Green;
                    c.BorderThickness = new Thickness(2, 2, 2, 2);
                });

                Task.Factory.StartNew(() =>
                {
                    Task.Delay(1000).Wait();

                    Utility.Utility.UIDispatcher.BeginInvoke(() =>
                    {
                        c.BorderBrush = old;
                        c.BorderThickness = size;
                    });
                });
            }
        }

        private void SavePluginList()
        {
            if (selectedProfile != null)
            {
                List<string> list = new List<string>();

                foreach (var entry in EntryPluginList.Items)
                {
                    list.Add(((ListBoxItem)entry).Content.ToString());
                }

                selectedProfile.CUOSettings.Plugins = list.ToArray();
                selectedProfile.SaveAsync(EntryPluginList);
            }
        }

        private void SetEntries(Profile profile)
        {
            EntryProfileName.Text = profile.Name;

            EntryAccountName.Text = profile.CUOSettings.Username;
            EntryAccountPass.Password = Crypter.Decrypt(profile.CUOSettings.Password);
            EntrySavePass.IsChecked = profile.CUOSettings.SaveAccount;

            EntryServerIP.Text = profile.CUOSettings.IP;
            EntryServerPort.Text = profile.CUOSettings.Port.ToString();

            EntryUODirectory.Text = profile.CUOSettings.UltimaOnlineDirectory;
            EntryClientVersion.Text = profile.CUOSettings.ClientVersion;
            EntryEncrypedClient.IsChecked = profile.CUOSettings.Encryption == 0 ? false : true;

            EntryPluginList.Items.Clear();
            foreach (var entry in profile.CUOSettings.Plugins)
            {
                EntryPluginList.Items.Add(new ListBoxItem() { Content = entry });
            }

            EntryAutoLogin.IsChecked = profile.CUOSettings.AutoLogin;
            EntryReconnect.IsChecked = profile.CUOSettings.Reconnect;
            EntryReconnectTime.Text = profile.CUOSettings.ReconnectTime.ToString();
            EntryLoginMusic.IsChecked = profile.CUOSettings.LoginMusic;
            EntryMusicVolume.Value = profile.CUOSettings.LoginMusicVolume;

            EntryLastCharName.Text = profile.LastCharacterName;
            EntryAdditionalArgs.Text = profile.AdditionalArgs;
        }

        private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileList.SelectedIndex > -1)
            {
                if (ProfileManager.TryFindProfile(((ListBoxItem)ProfileList.SelectedItem).Content.ToString(), out Profile profile))
                {
                    if (profile != null)
                    {
                        selectedProfile = profile;
                        SetEntries(profile);
                        ProfileEditArea.IsEnabled = true;
                    }
                }
            }
        }

        private void ButtonCopy_MouseUp(object sender, RoutedEventArgs e)
        {
            if (selectedProfile != null)
            {
                Profile copy = new Profile();
                copy.OverrideSettings(selectedProfile.CUOSettings);

                while(ProfileManager.TryFindProfile(copy.Name, out _))
                {
                    copy.Name += "x";
                }

                copy.Save();

                ProfileList.Items.Add(new ListBoxItem() { Content = copy.Name });
                ProfileList.SelectedIndex = ProfileList.Items.Count - 1;
            }
        }

        private void ButtonDelete_MouseUp(object sender, RoutedEventArgs e)
        {
            if (selectedProfile != null)
            {
                int indx = ProfileList.SelectedIndex;

                ProfileManager.DeleteProfileFile(selectedProfile, true);

                ProfileList.Items.RemoveAt(indx);

                selectedProfile = null;

                ProfileEditArea.IsEnabled = false;
            }
        }

        private void ButtonNew_MouseUp(object sender, RoutedEventArgs e)
        {
            Profile profile = new Profile();
            while (ProfileManager.TryFindProfile(profile.Name, out _))
            {
                profile.Name += "x";
            }
            profile.Save();
            ProfileList.Items.Add(new ListBoxItem() { Content = profile.Name });
            ProfileList.SelectedIndex = ProfileList.Items.Count - 1;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) //Loose focus on textbox
        {
            MainCanvas.Focus();
        }



        private void ReconnectUP(object sender, RoutedEventArgs e)
        {
            if (selectedProfile != null && int.TryParse(EntryReconnectTime.Text, out int v))
            {
                v += 100;
                EntryReconnectTime.Text = v.ToString();
                selectedProfile.CUOSettings.ReconnectTime = v;
                selectedProfile.SaveAsync(EntryReconnectTime);
            }
        }

        private void ReconnectDOWN(object sender, RoutedEventArgs e)
        {
            if (selectedProfile != null && int.TryParse(EntryReconnectTime.Text, out int v))
            {
                v -= 100;
                EntryReconnectTime.Text = v.ToString();
                selectedProfile.CUOSettings.ReconnectTime = v;
                selectedProfile.SaveAsync(EntryReconnectTime);
            }
        }
    }
}
