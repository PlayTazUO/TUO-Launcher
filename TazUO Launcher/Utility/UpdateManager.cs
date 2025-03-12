using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TazUO_Launcher.Utility
{
    class UpdateManager
    {
        private const string UPDATE_ZIP_URL = "https://github.com/bittiez/TazUO/releases/latest/download/TazUO.zip";

        public static UpdateManager Instance { get; private set; } = new UpdateManager();
        public bool DownloadInProgress { get; private set; } = false;
        public Version RemoteVersion { get; private set; } = null;
        public Version RemoteLauncherVersion { get; private set; } = null;
        public Version LocalVersion { get; private set; } = null;
        public GitHubReleaseData MainReleaseData { get; private set; } = null;
        public GitHubReleaseData LauncherReleaseData { get; private set; } = null;

        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Download the most recent version of TazUO
        /// </summary>
        /// <param name="action">This method is called using the ui dispatcher</param>
        /// <param name="afterCompleted">This method is called on the download thread</param>
        /// <returns></returns>
        public Task DownloadTUO(Action<int>? action = null, Action afterCompleted = null)
        {
            if (DownloadInProgress)
            {
                return Task.CompletedTask;
            }

            DownloadProgress downloadProgress = new DownloadProgress();

            downloadProgress.DownloadProgressChanged += (s, e) =>
            {
                Utility.UIDispatcher.InvokeAsync(() =>
                {
                    action?.Invoke((int)(downloadProgress.ProgressPercentage * 100));
                });
            };

            Task download = Task.Factory.StartNew(() =>
            {
                string tempFilePath = Path.GetTempFileName();
                using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (MainReleaseData != null)
                    {
                        foreach (GitHubReleaseData.Asset asset in MainReleaseData.assets)
                        {
                            if (
                                asset.name.EndsWith(".zip") &&
                                (asset.name.StartsWith("ClassicUO") || asset.name.StartsWith("TazUO"))
                                )
                            {
                                httpClient.DownloadAsync(asset.browser_download_url, file, downloadProgress).Wait();
                                break;
                            }
                        }
                    }
                    else
                    {
                        httpClient.DownloadAsync(UPDATE_ZIP_URL, file, downloadProgress).Wait();
                    }
                }

                try
                {
                    Directory.CreateDirectory(LauncherSettings.TazUOPath);

                    TrySetDirectoryFullPermissions(LauncherSettings.TazUOPath);

                    ZipFile.ExtractToDirectory(tempFilePath, LauncherSettings.TazUOPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                afterCompleted?.Invoke();
                DownloadInProgress = false;
            });

            return download;
        }

        public Task DownloadLauncher(Action<int>? action = null)
        {
            if (DownloadInProgress)
            {
                return Task.CompletedTask;
            }

            DownloadProgress downloadProgress = new DownloadProgress();

            downloadProgress.DownloadProgressChanged += (s, e) =>
            {
                Utility.UIDispatcher.InvokeAsync(() =>
                {
                    action?.Invoke((int)(downloadProgress.ProgressPercentage * 100));
                });
            };

            Task download = Task.Factory.StartNew(() =>
            {
                string tempFilePath = Path.Combine(Path.GetTempPath(), "tuo.launcher.zip");
                using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (LauncherReleaseData != null)
                    {
                        foreach (GitHubReleaseData.Asset asset in LauncherReleaseData.assets)
                        {
                            if (
                                asset.name.EndsWith(".zip") &&
                                (asset.name.StartsWith("Launcher") || asset.name.StartsWith("TazUO"))
                                )
                            {
                                httpClient.DownloadAsync(asset.browser_download_url, file, downloadProgress).Wait();

                                MessageBox.Show(
                                    "Quick guide:\n" +
                                    "1. The launcher will now close\n" +
                                    "2. The launcher folder will open, and the new update zip will open\n" +
                                    "3. Move the contents of the zip folder to the launcher folder\n" +
                                    "4. Re-open the launcher!"
                                    );

                                Process.Start("explorer.exe", System.AppDomain.CurrentDomain.BaseDirectory);
                                Process.Start("explorer.exe", tempFilePath);

                                Utility.UIDispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
                            }
                        }
                    }
                }



                DownloadInProgress = false;

            });

            return download;
        }

        public Task DownloadLatestBleedingEdge(Action<int>? action = null, Action afterCompleted = null)
        {
            DownloadProgress downloadProgress = new DownloadProgress();

            downloadProgress.DownloadProgressChanged += (s, e) =>
            {
                Utility.UIDispatcher.InvokeAsync(() =>
                {
                    action?.Invoke((int)(downloadProgress.ProgressPercentage * 100));
                });
            };

            return Task.Factory.StartNew(() =>
            {
                string tempFilePath = Path.GetTempFileName();
                using (var file = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    GitHubReleaseData bleedingEdgeData = GetReleaseData("https://api.github.com/repos/bittiez/TazUO/releases/tags/TazUO-BleedingEdge");

                    if (bleedingEdgeData != null)
                    {
                        foreach (GitHubReleaseData.Asset asset in bleedingEdgeData.assets)
                        {
                            if (asset.name.EndsWith(".zip") && asset.name.StartsWith("TazUO"))
                            {
                                httpClient.DownloadAsync(asset.browser_download_url, file, downloadProgress).Wait();
                                break;
                            }
                        }
                    }
                    else
                    {
                        afterCompleted?.Invoke();
                        return;
                    }
                }

                try
                {
                    Directory.CreateDirectory(LauncherSettings.TazUOPath);

                    TrySetDirectoryFullPermissions(LauncherSettings.TazUOPath);

                    ZipFile.ExtractToDirectory(tempFilePath, LauncherSettings.TazUOPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                afterCompleted?.Invoke();
            });
        }

        public static void TrySetDirectoryFullPermissions(string path)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (directoryInfo.Exists)
                {
                    DirectorySecurity security = directoryInfo.GetAccessControl();
                    security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Modify, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    directoryInfo.SetAccessControl(security);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void GetRemoteVersionAsync(Action? onVersionFound = null, string url = "https://api.github.com/repos/bittiez/TazUO/releases/latest")
        {
            Task.Factory.StartNew(() =>
            {
                MainReleaseData = GetReleaseData(url);

                if (MainReleaseData != null)
                {
                    if (MainReleaseData.name.StartsWith("v"))
                    {
                        MainReleaseData.name = MainReleaseData.name.Substring(1);
                    }

                    if (Version.TryParse(MainReleaseData.name, out var version))
                    {
                        RemoteVersion = version;
                        Utility.UIDispatcher.InvokeAsync(() =>
                        {
                            onVersionFound?.Invoke();
                        });
                    }
                }
            });
        }

        private GitHubReleaseData GetReleaseData(string url)
        {
            HttpRequestMessage restApi = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };
            restApi.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            restApi.Headers.Add("User-Agent", "Public");
            string jsonResponse = httpClient.Send(restApi).Content.ReadAsStringAsync().Result;

            Console.WriteLine(jsonResponse);

            return JsonSerializer.Deserialize<GitHubReleaseData>(jsonResponse);
        }

        public void GetRemoteLauncherVersionAsync(Action? onVersionFound = null)
        {
            Task.Factory.StartNew(() =>
            {
                HttpRequestMessage restApi = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://api.github.com/repos/bittiez/TUO-Launcher/releases/latest"),
                };
                restApi.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                restApi.Headers.Add("User-Agent", "Public");
                string jsonResponse = httpClient.Send(restApi).Content.ReadAsStringAsync().Result;

                Console.WriteLine(jsonResponse);

                LauncherReleaseData = JsonSerializer.Deserialize<GitHubReleaseData>(jsonResponse);

                if (LauncherReleaseData != null)
                {
                    if (LauncherReleaseData.tag_name.StartsWith("v"))
                    {
                        LauncherReleaseData.tag_name = LauncherReleaseData.tag_name.Substring(1);
                    }

                    if (Version.TryParse(LauncherReleaseData.tag_name, out var version))
                    {
                        RemoteLauncherVersion = version;
                        Utility.UIDispatcher.InvokeAsync(() =>
                        {
                            onVersionFound?.Invoke();
                        });
                    }
                }
            });
        }

        public Version? GetInstalledVersion(string exePath)
        {
            if (File.Exists(exePath))
            {
                Version v = AssemblyName.GetAssemblyName(exePath).Version;
                if (v != null)
                {
                    return LocalVersion = AssemblyName.GetAssemblyName(exePath).Version;
                }
            }
            return null;
        }

        public class DownloadProgress : IProgress<float>
        {
            public event EventHandler DownloadProgressChanged;

            public float ProgressPercentage { get; set; }

            public void Report(float value)
            {
                ProgressPercentage = value;
                DownloadProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync())
                {

                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                    progress.Report(1);
                }
            }
        }
    }

    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }
}
