using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace TazUOLauncher;

internal static class UpdateHelper
{
    public static ConcurrentDictionary<ReleaseChannel, GitHubReleaseData> ReleaseData = new();

    public static bool HaveData(ReleaseChannel channel) { return ReleaseData.ContainsKey(channel) && ReleaseData[channel] != null; }

    public static async Task GetAllReleaseData(ReleaseChannel priorityChannel)
    {
        if (BuildInfo.IsDebug)
        {
            var d = new GitHubReleaseData() { name = "Version v10.10.10.0"};
            if (!ReleaseData.TryAdd(priorityChannel, d))
                ReleaseData[priorityChannel] = d;
            return;
        }
        
        await TryGetReleaseData(priorityChannel);
        await Task.Delay(1000);
        await TryGetReleaseData(ReleaseChannel.LAUNCHER);
    }

    private static async Task<GitHubReleaseData?> TryGetReleaseData(ReleaseChannel channel)
    {
        string url;
        
        Console.WriteLine($"Grabbing release data for {channel}...");

        switch (channel)
        {
            case ReleaseChannel.MAIN:
                url = CONSTANTS.MAIN_CHANNEL_RELEASE_URL;
                break;
            case ReleaseChannel.DEV:
                url = CONSTANTS.DEV_CHANNEL_RELEASE_URL;
                break;
            case ReleaseChannel.LAUNCHER:
                url = CONSTANTS.LAUNCHER_RELEASE_URL;
                break;
            case ReleaseChannel.NET472:
                url = CONSTANTS.NET472_CHANNEL_RELEASE_URL;
                break;
            default:
                url = CONSTANTS.MAIN_CHANNEL_RELEASE_URL;
                break;
        }

        return await Task.Run(async () =>
        {
            var d = await TryGetReleaseData(url);

            if (d != null)
                if (!ReleaseData.TryAdd(channel, d))
                    ReleaseData[channel] = d;

            return d;
        });
    }

    private static async Task<GitHubReleaseData?> TryGetReleaseData(string url)
    {
        HttpRequestMessage restApi = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
        };
        restApi.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        restApi.Headers.Add("User-Agent", "Public");

        try
        {
            using var httpClient = new HttpClient();
            string jsonResponse = await httpClient.Send(restApi).Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubReleaseData>(jsonResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public static async Task<string> GetNews(ReleaseChannel channel)
    {
        string chan = "dev";
        switch (channel)
        {
            case ReleaseChannel.MAIN:
                chan = "main";
                break;
            case ReleaseChannel.NET472:
                chan = "legacy";
                break;
        }
        string url = string.Format(CONSTANTS.CHANGE_LOG_URL, chan);
        
        Console.WriteLine($"Grabbing changelog from {channel} channel...");

        try
        {
            using var client = new HttpClient();
            string rawResponse = await client.GetStringAsync(url);
            
            if (rawResponse.Length > 8000)
                rawResponse = rawResponse.Substring(0, 8000) + $"... \n For more see {url}";
            
            return rawResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "Unable to retrieve news..";
        }
    }

    /// <summary>Downloads the launcher ZIP for the current platform to a temp file. Returns the temp file path, or null on failure.</summary>
    public static async Task<string?> DownloadLauncherZip(DownloadProgress downloadProgress)
    {
        if (!HaveData(ReleaseChannel.LAUNCHER)) return null;

        GitHubReleaseData releaseData = ReleaseData[ReleaseChannel.LAUNCHER];

        if (releaseData == null || releaseData.assets == null) return null;

        string platformZipName = PlatformHelper.GetPlatformZipName();

        GitHubReleaseData.Asset? selectedAsset = null;
        foreach (GitHubReleaseData.Asset asset in releaseData.assets)
        {
            if (asset.name != null && asset.name.EndsWith(platformZipName) && asset.browser_download_url != null)
            {
                selectedAsset = asset;
                break;
            }
        }

        if (selectedAsset == null) return null;

        try
        {
            string tempFilePath = Path.GetTempFileName();
            using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                HttpClient httpClient = new HttpClient();
                await httpClient.DownloadAsync(selectedAsset.browser_download_url, file, downloadProgress);
            }
            return tempFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    /// <summary>
    /// Only supports dev/main not launcher channel
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="downloadProgress"></param>
    /// <param name="onCompleted"></param>
    /// <param name="parentWindow"></param>
    public static async void DownloadAndInstallZip(ReleaseChannel channel, DownloadProgress downloadProgress, Action onCompleted, Window? parentWindow = null)
    {
        if (!HaveData(channel)) return;

        if (Process.GetProcessesByName("TazUO").Length > 0)
        {
            if (parentWindow != null)
            {
                bool proceed = await Utility.ShowConfirmationDialog(
                    parentWindow,
                    "TazUO is Running",
                    "TazUO appears to be running. Updating while the client is running may cause issues.\n\nDo you want to proceed with the update anyway?"
                );

                if (!proceed)
                {
                    onCompleted();
                    return;
                }
            }
            else
            {
                onCompleted();
                return;
            }
        }

        GitHubReleaseData releaseData = ReleaseData[channel];

        if (releaseData == null || releaseData.assets == null)
        {
            _ = TryGetReleaseData(channel);
            return;
        }

        string extractTo = PathHelper.ClientPath;

        await Task.Run(() =>
        {
            GitHubReleaseData.Asset? selectedAsset = null;
            string platformZipName = PlatformHelper.GetPlatformZipName();
            
            // First, try to find platform-specific zip
            foreach (GitHubReleaseData.Asset asset in releaseData.assets)
            {
                if (asset.name != null && asset.name.EndsWith(platformZipName) && asset.browser_download_url != null)
                {
                    selectedAsset = asset;
                    break;
                }
            }
            
            // Fallback to current method if platform-specific zip not found
            if (selectedAsset == null)
            {
                foreach (GitHubReleaseData.Asset asset in releaseData.assets)
                {
                    if (asset.name != null && asset.name.EndsWith(".zip") && asset.name.StartsWith(CONSTANTS.ZIP_STARTS_WITH) && asset.browser_download_url != null)
                    {
                        selectedAsset = asset;
                        break;
                    }
                }
            }
            
            if (selectedAsset != null)
            {
                Console.WriteLine($"Picked for download: {selectedAsset.name} from {selectedAsset.browser_download_url}");
                try
                {
                    string tempFilePath = Path.GetTempFileName();
                    using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        HttpClient httpClient = new HttpClient();
                        httpClient.DownloadAsync(selectedAsset.browser_download_url, file, downloadProgress).Wait();
                    }

                    Directory.CreateDirectory(extractTo);
                    ZipFile.ExtractToDirectory(tempFilePath, extractTo, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            onCompleted?.Invoke();
        });
    }
}