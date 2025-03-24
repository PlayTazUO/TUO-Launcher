using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace TazUOLauncher;

public partial class ProfileEditorWindow : Window
{
    ProfileEditorViewModel viewModel;

    private Profile? selectedProfile;
    public ProfileEditorWindow()
    {
        InitializeComponent();

        DataContext = viewModel = new ProfileEditorViewModel();

        viewModel.Profiles = [.. ProfileManager.GetProfileNames()];

        EntryAccountName.TextChanged += (s, e) =>
        {
            if (!string.IsNullOrEmpty(EntryAccountName.Text))
                EntrySavePass.IsChecked = true;
        };
    }

    public void LocateUOFolderClicked(object s, RoutedEventArgs args)
    {
        Utility.OpenFolderDialog(this, "Select your UO folder").ContinueWith((f) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (selectedProfile == null) return;
                string res = f.Result;
                if (Directory.Exists(res))
                {
                    EntryUODirectory.Text = res;
                }
                else
                {
                    Console.WriteLine($"Folder doesn't exist: {res}");
                }
            });
        });
    }
    public void AddPluginClicked(object s, RoutedEventArgs args)
    {
        Utility.OpenFileDialog(this, "Select a plugin").ContinueWith((f) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (selectedProfile == null) return;
                string res = f.Result;
                if (File.Exists(res))
                {
                    viewModel.Plugins.Add(res);
                }
            });
        });
    }
    public void ProfileSelectionChanged(object s, SelectionChangedEventArgs args)
    {
        if (s == null || s is not ListBox profileListBox || profileListBox.SelectedItem == null)
        {
            viewModel.EditAreaEnabled = false;
            return;
        }

        if (profileListBox.SelectedItem is string si && si != null)
            if (ProfileManager.TryFindProfile(si, out selectedProfile) && selectedProfile != null)
            {
                PopulateProfileInfo();
                viewModel.EditAreaEnabled = true;
            }
    }
    public void SaveButtonClicked(object s, RoutedEventArgs args)
    {
        if (selectedProfile == null) return;

        if (EntryProfileName.Text != null)
            selectedProfile.Name = EntryProfileName.Text;
        if (EntryAccountName.Text != null)
            selectedProfile.CUOSettings.Username = EntryAccountName.Text;
        if (EntryAccountPass.Text != null)
            selectedProfile.CUOSettings.Password = Crypter.Encrypt(EntryAccountPass.Text);
        if (EntrySavePass.IsChecked != null)
            selectedProfile.CUOSettings.SaveAccount = (bool)EntrySavePass.IsChecked;
        if (EntryServerIP.Text != null)
            selectedProfile.CUOSettings.IP = EntryServerIP.Text;
        if (ushort.TryParse(EntryServerPort.Text, out var r))
            selectedProfile.CUOSettings.Port = r;
        if (EntryUODirectory.Text != null)
            selectedProfile.CUOSettings.UltimaOnlineDirectory = EntryUODirectory.Text;
        if (EntryClientVersion.Text != null && ClientVersionHelper.IsClientVersionValid(EntryClientVersion.Text, out _))
            selectedProfile.CUOSettings.ClientVersion = EntryClientVersion.Text;
        if (EntryEncrypedClient.IsChecked != null)
            selectedProfile.CUOSettings.Encryption = (byte)((bool)EntryEncrypedClient.IsChecked ? 1 : 0);

        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        foreach (var entry in EntryPluginList.Items)
        {
            if (entry is string i && i != null)
                list.Add(i);
        }
        selectedProfile.CUOSettings.Plugins = list.ToArray();

        if (EntryAutoLogin.IsChecked != null)
            selectedProfile.CUOSettings.AutoLogin = (bool)EntryAutoLogin.IsChecked;
        if (EntryReconnect.IsChecked != null)
            selectedProfile.CUOSettings.Reconnect = (bool)EntryReconnect.IsChecked;

        if (int.TryParse(EntryReconnectTime.Text, out var rt))
            selectedProfile.CUOSettings.ReconnectTime = rt;

        if (EntryLoginMusic.IsChecked != null)
            selectedProfile.CUOSettings.LoginMusic = (bool)EntryLoginMusic.IsChecked;
        selectedProfile.CUOSettings.LoginMusicVolume = (int)EntryMusicVolume.Value;

        if (EntryLastCharName.Text != null)
            selectedProfile.LastCharacterName = EntryLastCharName.Text;
        if (EntryAdditionalArgs.Text != null)
            selectedProfile.AdditionalArgs = EntryAdditionalArgs.Text;

        selectedProfile.Save();
        viewModel.Profiles = [.. ProfileManager.GetProfileNames()]; //Update names in list
    }
    public void NewProfileClicked(object s, RoutedEventArgs args)
    {
        Profile p = new Profile();
        p.Save();

        ProfileManager.AllProfiles = ProfileManager.AllProfiles.Append(p).ToArray();
        viewModel.Profiles = [.. ProfileManager.GetProfileNames()];
    }
    public void DeleteProfileClicked(object s, RoutedEventArgs args)
    {
        if (selectedProfile == null) return;

        ProfileManager.DeleteProfileFile(selectedProfile, true);
        ProfileManager.GetAllProfiles().Wait();
        viewModel.Profiles = [.. ProfileManager.GetProfileNames()];
    }
    public void CopyProfileClicked(object s, RoutedEventArgs args)
    {
        if (selectedProfile == null) return;
        Profile p = new Profile();
        p.Name = ProfileManager.EnsureUniqueName(selectedProfile.Name);
        p.LastCharacterName = selectedProfile.LastCharacterName;
        p.AdditionalArgs = selectedProfile.AdditionalArgs;

        var settings = selectedProfile.CUOSettings.GetSaveData();
        p.OverrideSettings(JsonSerializer.Deserialize<Settings>(settings) ?? new Settings());

        p.Save();
        ProfileManager.GetAllProfiles().Wait();
        viewModel.Profiles = [.. ProfileManager.GetProfileNames()];
    }
    public void PluginRemoveClicked(object s, RoutedEventArgs args)
    {
        if (selectedProfile == null) return;

        if (EntryPluginList.SelectedItem != null && EntryPluginList.SelectedItem is string selectedPlugin && viewModel.Plugins.Contains(selectedPlugin))
        {
            viewModel.Plugins.Remove(selectedPlugin);
        }
    }

    private void PopulateProfileInfo()
    {
        if (selectedProfile == null) return;

        EntryProfileName.Text = selectedProfile.Name;
        EntryAccountName.Text = selectedProfile.CUOSettings.Username;
        EntryAccountPass.Text = Crypter.Decrypt(selectedProfile.CUOSettings.Password);
        EntrySavePass.IsChecked = selectedProfile.CUOSettings.SaveAccount;
        EntryServerIP.Text = selectedProfile.CUOSettings.IP;
        EntryServerPort.Text = selectedProfile.CUOSettings.Port.ToString();
        EntryUODirectory.Text = selectedProfile.CUOSettings.UltimaOnlineDirectory;
        EntryClientVersion.Text = selectedProfile.CUOSettings.ClientVersion;
        EntryEncrypedClient.IsChecked = selectedProfile.CUOSettings.Encryption == 0 ? false : true;

        viewModel.Plugins = [.. selectedProfile.CUOSettings.Plugins];

        EntryAutoLogin.IsChecked = selectedProfile.CUOSettings.AutoLogin;
        EntryReconnect.IsChecked = selectedProfile.CUOSettings.Reconnect;
        EntryReconnectTime.Text = selectedProfile.CUOSettings.ReconnectTime.ToString();
        EntryLoginMusic.IsChecked = selectedProfile.CUOSettings.LoginMusic;
        EntryMusicVolume.Value = selectedProfile.CUOSettings.LoginMusicVolume;

        EntryLastCharName.Text = selectedProfile.LastCharacterName;
        EntryAdditionalArgs.Text = selectedProfile.AdditionalArgs;

        selectedProfile.Save();
    }
}

public class ProfileEditorViewModel : INotifyPropertyChanged
{
    private ObservableCollection<string> profiles = new ObservableCollection<string>();
    private ObservableCollection<string> plugins = new ObservableCollection<string>();
    private bool editAreaEnabled;

    public ObservableCollection<string> Plugins
    {
        get => plugins;
        set
        {
            plugins = value;
            OnPropertyChanged(nameof(Plugins));
        }
    }
    public ObservableCollection<string> Profiles
    {
        get => profiles;
        set
        {
            profiles = value;
            OnPropertyChanged(nameof(Profiles));
        }
    }

    public bool EditAreaEnabled
    {
        get => editAreaEnabled; set
        {
            editAreaEnabled = value;
            OnPropertyChanged(nameof(EditAreaEnabled));
        }
    }
    public ProfileEditorViewModel()
    {
        Profiles = new ObservableCollection<string>() { };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}