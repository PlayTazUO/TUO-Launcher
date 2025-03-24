using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TazUO_Launcher;

namespace TazUOLauncher;

public partial class MainWindow : Window
{
    private MainWindowViewModel viewModel;
    private ClientStatus clientStatus = ClientStatus.INITIALIZING;
    private Queue<ReleaseChannel> updatesAvailable = new Queue<ReleaseChannel>();
    private ReleaseChannel nextDownloadType = ReleaseChannel.INVALID;

    private Profile? selectedProfile;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = viewModel = new MainWindowViewModel();

        DoChecksAsync();
        LoadProfiles();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        foreach (Profile p in ProfileManager.AllProfiles)
            p?.Save();

        base.OnClosing(e);
    }
    private async void LoadProfiles()
    {
        await ProfileManager.GetAllProfiles();
        SetProfileSelectorComboBox();
    }
    private async void DoChecksAsync()
    {
        var remoteVersionInfo = UpdateHelper.GetAllReleaseData();
        ClientExistsChecks(); //Doesn't need to wait for release data

        await remoteVersionInfo; //Things after this are waiting for release data
        UpdateVersionStrings();
        CheckLauncherVersion();
        ClientUpdateChecks();
        HandleUpdates();
    }
    private void SetProfileSelectorComboBox()
    {
        viewModel.Profiles = [CONSTANTS.EDIT_PROFILES, .. ProfileManager.GetProfileNames()];
    }
    private void CheckLauncherVersion()
    {
        if (UpdateHelper.HaveData(ReleaseChannel.LAUNCHER))
        {
            var data = UpdateHelper.ReleaseData[ReleaseChannel.LAUNCHER];
            if (data.GetVersion() > LauncherVersion.GetLauncherVersion())
            {
                //Update available, do something
                viewModel.DangerNoticeString = $"A launcher update is available! For now they must be manually downloaded. {LauncherVersion.GetLauncherVersion().ToHumanReable()} -> {data.GetVersion().ToHumanReable()}";
            }
        }
    }
    private void UpdateVersionStrings()
    {
        if (UpdateHelper.HaveData(ReleaseChannel.MAIN))
            viewModel.RemoteVersionString = string.Format(CONSTANTS.REMOTE_VERSION_FORMAT, UpdateHelper.ReleaseData[ReleaseChannel.MAIN].GetVersion().ToHumanReable());
    }
    private void ClientExistsChecks()
    {
        if (!ClientHelper.ExecutableExists())
        {
            viewModel.DangerNoticeString = "No client found, we need to download one!";
            viewModel.LocalVersionString = string.Format(CONSTANTS.LOCAL_VERSION_FORMAT, "N/A");
            clientStatus = ClientStatus.NO_LOCAL_CLIENT;
            updatesAvailable.Enqueue(ReleaseChannel.MAIN);
        }
        else
        {
            viewModel.DangerNoticeString = string.Empty;
            viewModel.LocalVersionString = string.Format(CONSTANTS.LOCAL_VERSION_FORMAT, ClientHelper.LocalClientVersion.ToHumanReable());
            viewModel.PlayButtonEnabled = true;
            clientStatus = ClientStatus.READY;
        }
    }
    private void ClientUpdateChecks()
    {
        if (clientStatus > ClientStatus.NO_LOCAL_CLIENT) //Only check for updates if we have a client insalled already
            if (UpdateHelper.HaveData(ReleaseChannel.MAIN))
            {
                if (UpdateHelper.ReleaseData[ReleaseChannel.MAIN].GetVersion() > ClientHelper.LocalClientVersion)
                {
                    updatesAvailable.Enqueue(ReleaseChannel.MAIN);
                }
            }
    }
    private void HandleUpdates()
    {
        nextDownloadType = ReleaseChannel.INVALID;
        if (updatesAvailable.TryDequeue(out var updateType))
        {
            switch (updateType)
            {
                case ReleaseChannel.MAIN:
                    viewModel.UpdateButtonString = clientStatus == ClientStatus.NO_LOCAL_CLIENT ? CONSTANTS.NO_CLIENT_AVAILABLE : CONSTANTS.CLIENT_UPDATE_AVAILABLE;
                    viewModel.ShowDownloadAvailableButton = true;
                    nextDownloadType = ReleaseChannel.MAIN;
                    break;
                case ReleaseChannel.LAUNCHER:
                    viewModel.UpdateButtonString = CONSTANTS.LAUNCHER_UPDATE_AVAILABLE;
                    viewModel.ShowDownloadAvailableButton = true;
                    nextDownloadType = ReleaseChannel.LAUNCHER;
                    break;
            }
        }
    }
    private void DoNextDownload()
    {
        if (nextDownloadType == ReleaseChannel.INVALID || clientStatus == ClientStatus.DOWNLOAD_IN_PROGRESS) return;

        viewModel.ShowDownloadAvailableButton = false;
        var prog = new DownloadProgress();
        prog.DownloadProgressChanged += (_, _) =>
        {
            Dispatcher.UIThread.InvokeAsync(() => viewModel.DownloadProgressBarPercent = (int)(prog.ProgressPercentage * 100));
        };

        viewModel.PlayButtonEnabled = false;
        clientStatus = ClientStatus.DOWNLOAD_IN_PROGRESS;
        viewModel.ShowDownloadAvailableButton = false;
        viewModel.DownloadProgressBarPercent = 0;
        viewModel.ShowDownloadProgressBar = true;

        UpdateHelper.DownloadAndInstallZip(nextDownloadType, prog, () =>
        {
            viewModel.ShowDownloadProgressBar = false;
            ClientHelper.LocalClientVersion = ClientHelper.LocalClientVersion; //Client version is re-checked when setting this var
            ClientExistsChecks();
            HandleUpdates();
        });
    }
    private void OpenEditProfiles(){
        viewModel.DangerNoticeString = "Tried to open profile editor, it's not set up yet.";
    }

    public void PlayButtonClicked(object sender, RoutedEventArgs args)
    {
        ClientHelper.TrySetPlusXUnix();
        if (selectedProfile != null)
            Utility.LaunchClient(selectedProfile);
    }
    public void DownloadButtonClicked(object sender, RoutedEventArgs args)
    {
        DoNextDownload();
    }
    public void ProfileSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        var dd = ((ComboBox)sender);
        if (dd == null) return;

        if (dd.SelectedIndex == 0)
        { //Edit Profile
            OpenEditProfiles();
        }
        else if (dd.SelectedItem != null && dd.SelectedItem is string)
        {
            string si = (string)dd.SelectedItem;
            if (si != null && si != null)
                ProfileManager.TryFindProfile(si, out selectedProfile);
        }
    }
    public void EditProfilesClicked(object sender, RoutedEventArgs args){
        OpenEditProfiles();
    }
    public void OpenWikiClicked(object sender, RoutedEventArgs args)
    {
        WebLinks.OpenURLInBrowser(CONSTANTS.WIKI_URL);
    }
    public void OpenDiscordClicked(object sender, RoutedEventArgs args)
    {
        WebLinks.OpenURLInBrowser(CONSTANTS.DISCORD_URL);
    }
    public void OpenGithubClicked(object sender, RoutedEventArgs args)
    {
        WebLinks.OpenURLInBrowser(CONSTANTS.GITHUB_URL);
    }
    public void DownloadMainBuildClick(object sender, RoutedEventArgs args)
    {
        if (clientStatus == ClientStatus.DOWNLOAD_IN_PROGRESS) return;
        nextDownloadType = ReleaseChannel.MAIN;
        DoNextDownload();
    }
    public void DownloadDevBuildClick(object sender, RoutedEventArgs args)
    {
        if (clientStatus == ClientStatus.DOWNLOAD_IN_PROGRESS) return;
        nextDownloadType = ReleaseChannel.DEV;
        DoNextDownload();
    }
    public void ImportCUOLauncherClick(object sender, RoutedEventArgs args)
    {

    }
    public void ToolsButtonClick(object sender, RoutedEventArgs args)
    {
        ((Button)sender)?.ContextMenu?.Open();
        args.Handled = true;
    }
}

public class MainWindowViewModel : INotifyPropertyChanged
{
    private ObservableCollection<string> profiles = new ObservableCollection<string>();
    private bool showDownloadProgressBar;
    private int downloadProgressBarPercent;
    private bool showDownloadAvailableButton;
    private string remoteVersionString = string.Format(CONSTANTS.REMOTE_VERSION_FORMAT, "Checking...");
    private string localVersionString = "Local Version: Checking...";
    private string localLauncherVersionString = $"Launcher Version: {LauncherVersion.GetLauncherVersion().ToHumanReable()}";
    private string dangerNoticeString = string.Empty;
    private bool playButtonEnabled;
    private string updateButtonString = string.Empty;

    public ObservableCollection<string> Profiles
    {
        get => profiles;
        set
        {
            profiles = value;
            OnPropertyChanged(nameof(Profiles));
        }
    }

    public bool ShowDownloadProgressBar
    {
        get => showDownloadProgressBar;
        set
        {
            showDownloadProgressBar = value;
            OnPropertyChanged(nameof(ShowDownloadProgressBar));
        }
    }

    public int DownloadProgressBarPercent
    {
        get => downloadProgressBarPercent;
        set
        {
            downloadProgressBarPercent = value;
            if (downloadProgressBarPercent > 100)
                downloadProgressBarPercent = 100;
            if (downloadProgressBarPercent < 0)
                downloadProgressBarPercent = 0;
            OnPropertyChanged(nameof(DownloadProgressBarPercent));
        }
    }

    public bool ShowDownloadAvailableButton
    {
        get => showDownloadAvailableButton;
        set
        {
            showDownloadAvailableButton = value;
            OnPropertyChanged(nameof(ShowDownloadAvailableButton));
        }
    }

    public string RemoteVersionString
    {
        get => remoteVersionString; set
        {
            remoteVersionString = value;
            OnPropertyChanged(nameof(RemoteVersionString));
        }
    }

    public string LocalVersionString
    {
        get => localVersionString; set
        {
            localVersionString = value;
            OnPropertyChanged(nameof(LocalVersionString));
        }
    }

    public string LocalLauncherVersionString
    {
        get => localLauncherVersionString; set
        {
            localLauncherVersionString = value;
            OnPropertyChanged(nameof(LocalLauncherVersionString));
        }
    }

    public string DangerNoticeString
    {
        get => dangerNoticeString; set
        {
            dangerNoticeString = value;
            OnPropertyChanged(nameof(DangerNoticeString));
        }
    }

    public bool PlayButtonEnabled
    {
        get => playButtonEnabled; set
        {
            playButtonEnabled = value;
            OnPropertyChanged(nameof(PlayButtonEnabled));
        }
    }

    public string UpdateButtonString
    {
        get => updateButtonString; set
        {
            updateButtonString = value;
            OnPropertyChanged(nameof(UpdateButtonString));
        }
    }
    public MainWindowViewModel()
    {
        Profiles = new ObservableCollection<string>() { CONSTANTS.EDIT_PROFILES };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}