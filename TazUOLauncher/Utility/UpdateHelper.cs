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
    public static ConcurrentDictionary<ReleaseChannel, GitHubReleaseData> ReleaseData = new ConcurrentDictionary<ReleaseChannel, GitHubReleaseData>();

    public static bool HaveData(ReleaseChannel channel) { return ReleaseData.ContainsKey(channel) && ReleaseData[channel] != null; }

    public static async Task GetAllReleaseData()
    {
        List<Task> all = new List<Task>(){
            TryGetReleaseData(ReleaseChannel.DEV),
            Task.Delay(500),
            TryGetReleaseData(ReleaseChannel.MAIN),
            Task.Delay(500),
            TryGetReleaseData(ReleaseChannel.LAUNCHER),
            Task.Delay(500),
            TryGetReleaseData(ReleaseChannel.NET472),
        };

        await Task.WhenAll(all);
    }

    private static async Task<GitHubReleaseData?> TryGetReleaseData(ReleaseChannel channel)
    {
        string url;

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
            var httpClient = new HttpClient();
            string jsonResponse = await httpClient.Send(restApi).Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GitHubReleaseData>(jsonResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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